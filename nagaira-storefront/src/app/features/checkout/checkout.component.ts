import { Component, inject, signal, OnInit, effect, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { CartService } from '../../core/services/cart.service';
import { AuthService } from '../../core/services/auth.service';
import { OrderService } from '../../core/services/order.service';
import { PaymentMethodService, PaymentMethod } from '../../core/services/payment-method.service';
import { AppSettingsService } from '../../core/services/app-settings.service';
import { AppCurrencyPipe } from '../../core/pipes/currency.pipe';
import { CreateOrderRequest, Order, Product } from '../../core/models/models';
import { getProductPriceByQuantity } from '../../core/utils/product.utils';
import { AnalyticsService } from '../../core/services/analytics.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, AppCurrencyPipe],
  templateUrl: './checkout.component.html',
  styleUrls: ['./checkout.component.css']
})
export class CheckoutComponent implements OnInit {
  private readonly freeShippingThreshold = 700;
  private readonly standardShippingCost = 100;

  private readonly fallbackTransferPaymentMethod: PaymentMethod = {
    id: 'bank-transfer-fallback',
    name: 'Transferencia bancaria',
    description: 'Depósito o transferencia bancaria',
    type: 'BankAccount',
    typeLabel: 'Transferencia',
    accountNumber: '',
    bankName: '',
    accountHolderName: '',
    walletProvider: '',
    walletNumber: '',
    qrCodeUrl: '',
    instructions: 'Después de realizar el depósito, envía el comprobante por WhatsApp para confirmar tu pago.',
    displayOrder: 999,
    isActive: true
  };

  cartService = inject(CartService);
  authService = inject(AuthService);
  orderService = inject(OrderService);
  paymentMethodService = inject(PaymentMethodService);
  appSettings = inject(AppSettingsService);
  analyticsService = inject(AnalyticsService);
  router = inject(Router);
  fb = inject(FormBuilder);

  loading = signal(false);
  sessionReady = signal(false);
  error = signal('');
  success = signal(false);
  createdOrderId = signal('');
  createdOrderNumber = signal('');
  createdOrderTotal = signal<number | null>(null);
  paymentMethods = signal<PaymentMethod[]>([]);
  selectedPaymentMethod = signal<PaymentMethod | null>(null);
  selectedPaymentProofFile = signal<File | null>(null);
  selectedPaymentProofFileName = signal('');
  paymentProofImageUrl = signal('');
  savingPaymentProof = signal(false);
  isAuthenticated = computed(() => this.authService.isAuthenticated());
  currentUser = computed(() => this.authService.currentUser());
  hasProfileContact = computed(() => {
    const user = this.currentUser();
    if (!user) return false;
    const hasPhone = Boolean((user.phoneNumber || '').trim());
    return hasPhone;
  });
  displayName = computed(() => {
    const user = this.currentUser();
    if (!user) return 'Cliente';
    const fullName = `${user.firstName ?? ''} ${user.lastName ?? ''}`.trim();
    return fullName || 'Cliente';
  });
  discountTotal = computed(() => {
    if (!this.isAuthenticated()) return 0;
    return this.cartService.cartItems().reduce((total, item) => {
      const base = getProductPriceByQuantity(item.product, item.quantity);
      const offer = this.getItemOfferPrice(item.product);
      if (offer === null || offer >= base) return total;
      return total + ((base - offer) * item.quantity);
    }, 0);
  });

  discountedTotal = computed(() => {
    const total = this.cartService.total();
    const discount = this.discountTotal();
    return Math.max(total - discount, 0);
  });

  discountedSubtotal = computed(() => {
    const rate = this.appSettings.taxRate();
    const divisor = 1 + rate;
    if (divisor <= 0) return this.discountedTotal();
    return this.discountedTotal() / divisor;
  });

  discountedTax = computed(() => this.discountedTotal() - this.discountedSubtotal());
  qualifiesForFreeShipping = computed(() => this.discountedTotal() >= this.freeShippingThreshold);
  freeShippingRemaining = computed(() => Math.max(this.freeShippingThreshold - this.discountedTotal(), 0));
  shippingCost = computed(() => this.qualifiesForFreeShipping() ? 0 : this.standardShippingCost);
  orderGrandTotal = computed(() => this.discountedTotal() + this.shippingCost());
  freeShippingProgressMessage = computed(() => {
    const message = this.appSettings.getShippingFreeProgressMessage();
    return message.replaceAll('{amount}', this.formatMoney(this.freeShippingRemaining()));
  });
  whatsAppCheckoutUrl = computed(() => this.buildWhatsAppCheckoutUrl());
  selectedPaymentIsTransfer = computed(() => {
    const method = this.selectedPaymentMethod();
    return method ? this.isTransferPaymentMethod(method) : false;
  });
  selectedPaymentIsCashOnDelivery = computed(() => {
    const method = this.selectedPaymentMethod();
    return method ? this.isCashOnDeliveryPaymentMethod(method) : false;
  });
  paymentProofWhatsAppUrl = computed(() => this.buildPaymentProofWhatsAppUrl());

  shippingForm = this.fb.group({
    fullName: [''],
    email: ['', Validators.email],
    phone: ['', Validators.required],
    address: [''],
    city: [''],
    zipCode: [''],
    country: ['']
  });

  constructor() {
    effect(() => {
      if (this.appSettings.isSettingsLoaded()) {
        this.updateSettings();
      }
    });

    effect(() => {
      const user = this.currentUser();
      if (user) {
        this.prefillFromProfile();
      }
    });
  }

  async ngOnInit(): Promise<void> {
    await this.authService.waitForSessionReady();
    this.sessionReady.set(true);

    if (this.cartService.itemCount() === 0) {
      this.router.navigate(['/cart']);
      return;
    }

    this.updateSettings();
    this.loadPaymentMethods();
    if (this.cartService.itemCount() > 0) {
      this.analyticsService.beginCheckout(this.cartService.total(), this.cartService.itemCount());
    }
  }

  private prefillFromProfile(): void {
    const user = this.currentUser();
    if (!user) return;

    const fullName = `${user.firstName ?? ''} ${user.lastName ?? ''}`.trim();
    this.shippingForm.patchValue({
      fullName: fullName || '',
      email: user.email || '',
      phone: user.phoneNumber || ''
    });
  }

  private updateSettings(): void {
    this.shippingForm.patchValue({ country: this.appSettings.getDefaultCountry() });
  }

  loadPaymentMethods(): void {
    this.paymentMethodService.getActivePaymentMethods().subscribe({
      next: (methods) => {
        const paymentMethods = this.withTransferFallback(methods);
        this.paymentMethods.set(paymentMethods);
        if (paymentMethods.length > 0) {
          this.selectedPaymentMethod.set(paymentMethods[0]);
        }
      },
      error: (error) => {
        console.error('Error loading payment methods:', error);
        const paymentMethods = this.withTransferFallback([]);
        this.paymentMethods.set(paymentMethods);
        this.selectedPaymentMethod.set(paymentMethods[0]);
      }
    });
  }

  private withTransferFallback(methods: PaymentMethod[]): PaymentMethod[] {
    const activeMethods = Array.isArray(methods) ? methods : [];
    const hasTransferMethod = activeMethods.some(method => this.isTransferPaymentMethod(method));

    if (hasTransferMethod) {
      return activeMethods;
    }

    return [...activeMethods, this.fallbackTransferPaymentMethod];
  }

  getPaymentTypeLabel(method: PaymentMethod): string {
    return method.typeLabel || method.type || 'Otro';
  }

  getPaymentTypeIcon(method: PaymentMethod): string {
    const label = this.getPaymentTypeLabel(method).trim();
    return label.length > 0 ? label[0].toUpperCase() : '?';
  }

  isTransferPaymentMethod(method: PaymentMethod): boolean {
    const values = [
      method.name,
      method.description,
      method.type,
      method.typeLabel,
      method.bankName,
      method.accountNumber
    ]
      .filter(Boolean)
      .join(' ')
      .toLowerCase();

    return method.type === 'BankAccount'
      || values.includes('transfer')
      || values.includes('bancaria')
      || values.includes('banco');
  }

  hasAccountDetails(method: PaymentMethod): boolean {
    return Boolean(method.bankName || method.accountNumber || method.accountHolderName || method.walletProvider || method.walletNumber);
  }

  isCashOnDeliveryPaymentMethod(method: PaymentMethod): boolean {
    const values = [
      method.name,
      method.description,
      method.type,
      method.typeLabel
    ]
      .filter(Boolean)
      .join(' ')
      .toLowerCase();

    return method.type === 'Cash'
      || values.includes('contra entrega')
      || values.includes('efectivo');
  }

  selectPaymentMethod(method: PaymentMethod): void {
    this.selectedPaymentMethod.set(method);
  }

  onPaymentProofFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;

    if (!file) {
      this.selectedPaymentProofFile.set(null);
      this.selectedPaymentProofFileName.set('');
      return;
    }

    if (!file.type.startsWith('image/')) {
      this.error.set('Selecciona una imagen valida para el comprobante.');
      input.value = '';
      return;
    }

    if (file.size > 10 * 1024 * 1024) {
      this.error.set('El comprobante no puede ser mayor a 10MB.');
      input.value = '';
      return;
    }

    this.error.set('');
    this.paymentProofImageUrl.set('');
    this.selectedPaymentProofFile.set(file);
    this.selectedPaymentProofFileName.set(file.name);
  }

  async sendPaymentProofByWhatsApp(event?: Event): Promise<void> {
    event?.preventDefault();

    if (this.savingPaymentProof()) return;

    if (!this.sessionReady()) {
      this.error.set('Estamos validando tu sesion. Intenta nuevamente en unos segundos.');
      return;
    }

    const proofFile = this.selectedPaymentProofFile();
    if (!proofFile && !this.paymentProofImageUrl()) {
      this.error.set('Selecciona la imagen del comprobante antes de enviarlo por WhatsApp.');
      return;
    }

    let whatsappWindow: Window | null = null;

    try {
      this.savingPaymentProof.set(true);
      this.error.set('');

      let orderId = this.createdOrderId();
      if (!orderId) {
        if (this.shippingForm.invalid) {
          this.shippingForm.markAllAsTouched();
          this.error.set('Completa el telefono antes de registrar el pedido.');
          return;
        }
      }

      whatsappWindow = window.open('', '_blank');
      if (whatsappWindow) {
        whatsappWindow.opener = null;
      }

      if (!orderId) {
        const order = await this.createOrder();
        this.completeOrder(order);
        orderId = order.id;
      }

      const imageUrl = this.paymentProofImageUrl() || await this.uploadPaymentProofImage(proofFile!);
      await this.attachPaymentProof(orderId, imageUrl);
      const whatsappUrl = this.paymentProofWhatsAppUrl();
      if (whatsappWindow) {
        whatsappWindow.location.href = whatsappUrl;
      } else {
        window.location.href = whatsappUrl;
      }
    } catch (error: any) {
      whatsappWindow?.close();
      console.error('Error saving payment proof', error);
      this.error.set(error?.error?.message || 'No se pudo guardar el comprobante. Intenta nuevamente.');
    } finally {
      this.savingPaymentProof.set(false);
    }
  }

  private buildWhatsAppCheckoutUrl(): string {
    const shipping = this.shippingForm.getRawValue();
    const customerName = (shipping.fullName || this.displayName()).trim();
    const customerPhone = (shipping.phone || '').trim();
    const paymentMethod = this.selectedPaymentMethod();

    const lines = [
      'Hola, quiero completar mi pedido por WhatsApp.',
      '',
      'Productos:',
      ...this.cartService.cartItems().map(item => {
        const unitPrice = this.getItemDisplayPrice(item.product, item.quantity);
        const lineTotal = unitPrice * item.quantity;
        return `- ${item.quantity}x ${item.product.name} (${this.formatMoney(lineTotal)})`;
      }),
      '',
      `Total: ${this.formatMoney(this.orderGrandTotal())}`
    ];

    if (customerName) {
      lines.push(`Nombre: ${customerName}`);
    }

    if (customerPhone) {
      lines.push(`Teléfono: ${customerPhone}`);
    }

    if (paymentMethod) {
      lines.push(`Método de pago seleccionado: ${paymentMethod.name}`);
    }

    return `https://wa.me/${environment.whatsappCheckoutPhone}?text=${encodeURIComponent(lines.join('\n'))}`;
  }

  private buildPaymentProofWhatsAppUrl(): string {
    const shipping = this.shippingForm.getRawValue();
    const customerName = (shipping.fullName || this.displayName()).trim();
    const customerPhone = (shipping.phone || '').trim();
    const orderId = this.createdOrderNumber() || this.createdOrderId();
    const paymentMethod = this.selectedPaymentMethod();

    const lines = [
      'Hola, quiero enviar mi comprobante de pago por transferencia.',
      '',
      `Total pagado: ${this.formatMoney(this.createdOrderTotal() ?? this.orderGrandTotal())}`
    ];

    if (orderId) {
      lines.push(`Orden: #${orderId}`);
    }

    if (customerName) {
      lines.push(`Nombre: ${customerName}`);
    }

    if (customerPhone) {
      lines.push(`Teléfono: ${customerPhone}`);
    }

    if (paymentMethod) {
      lines.push(`Método de pago: ${paymentMethod.name}`);
    }

    lines.push('', 'Adjunto el comprobante en este chat.');

    return `https://wa.me/${environment.whatsappCheckoutPhone}?text=${encodeURIComponent(lines.join('\n'))}`;
  }

  private formatMoney(amount: number): string {
    const formatted = new Intl.NumberFormat('en-US', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(amount);

    return `${this.appSettings.getCurrencySymbol()} ${formatted}`;
  }

  async onSubmit(): Promise<void> {
    if (!this.sessionReady()) {
      this.error.set('Estamos validando tu sesión. Intenta nuevamente en unos segundos.');
      return;
    }

    if (this.shippingForm.invalid) {
      this.shippingForm.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set('');

    try {
      const order = await this.createOrder();
      this.completeOrder(order);
    } catch (err: any) {
      console.error('Checkout error', err);
      this.error.set(err.error?.message || 'Error al procesar la orden. Intente nuevamente.');
    } finally {
      this.loading.set(false);
    }
  }

  private buildOrderRequest(): CreateOrderRequest {
    const shipping = this.shippingForm.getRawValue();
    const method = this.selectedPaymentMethod();
    const methodId = method?.id && method.id !== this.fallbackTransferPaymentMethod.id ? method.id : undefined;

    return {
      items: this.cartService.cartItems().map(item => ({
        productId: item.product.id,
        quantity: item.quantity
      })),
      shippingAddressId: undefined,
      customerName: shipping.fullName!,
      customerEmail: shipping.email!,
      customerPhone: shipping.phone!,
      shippingStreet: shipping.address!,
      shippingCity: shipping.city!,
      shippingPostalCode: shipping.zipCode!,
      shippingCountry: shipping.country!,
      paymentMethodId: methodId,
      paymentMethodName: method ? method.name : undefined,
      paymentProofImageUrl: this.paymentProofImageUrl() || undefined
    };
  }

  private createOrder(): Promise<Order> {
    return firstValueFrom(this.orderService.createOrder(this.buildOrderRequest()));
  }

  private completeOrder(order: Order): void {
    this.cartService.clearCart();
    this.createdOrderId.set(order.id);
    this.createdOrderNumber.set(order.orderNumber || order.id);
    this.createdOrderTotal.set(order.total);
    this.success.set(true);

    const method = this.selectedPaymentMethod();
    this.analyticsService.purchase(
      order.id,
      order.total,
      this.appSettings.getCurrencySymbol(),
      {
        itemsCount: order.items?.length || 0,
        paymentMethod: method ? (method.name || this.getPaymentTypeLabel(method)) : undefined
      }
    );
  }

  private async uploadPaymentProofImage(file: File): Promise<string> {
    const response = await firstValueFrom(this.orderService.uploadPaymentProofImage(file));
    if (!response.imageUrl) {
      throw new Error('No se recibio la URL del comprobante.');
    }

    this.paymentProofImageUrl.set(response.imageUrl);
    return response.imageUrl;
  }

  private attachPaymentProof(orderId: string, imageUrl: string): Promise<Order> {
    const method = this.selectedPaymentMethod();
    const methodId = method?.id && method.id !== this.fallbackTransferPaymentMethod.id ? method.id : undefined;

    return firstValueFrom(this.orderService.updatePaymentProof(orderId, {
      paymentProofImageUrl: imageUrl,
      paymentMethodId: methodId,
      paymentMethodName: method ? method.name : undefined
    }));
  }

  getItemPrice(product: Product, quantity: number): number {
    return getProductPriceByQuantity(product, quantity);
  }

  getItemOfferPrice(product: Product): number | null {
    if (!this.isAuthenticated()) return null;
    if (typeof product.offerPrice !== 'number') return null;
    return product.offerPrice;
  }

  getItemDisplayPrice(product: Product, quantity: number): number {
    const base = getProductPriceByQuantity(product, quantity);
    const offer = this.getItemOfferPrice(product);
    if (offer !== null && offer < base) return offer;
    return base;
  }

  getTaxLabel(): string {
    return this.appSettings.taxLabel();
  }

  getShippingFreeLabel(): string {
    return this.appSettings.shippingFreeLabel();
  }
}
