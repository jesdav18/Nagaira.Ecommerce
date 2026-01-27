import { Component, inject, signal, OnInit, effect, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CartService } from '../../core/services/cart.service';
import { AuthService } from '../../core/services/auth.service';
import { OrderService } from '../../core/services/order.service';
import { PaymentMethodService, PaymentMethod } from '../../core/services/payment-method.service';
import { AppSettingsService } from '../../core/services/app-settings.service';
import { AppCurrencyPipe } from '../../core/pipes/currency.pipe';
import { Product } from '../../core/models/models';
import { getProductPrice } from '../../core/utils/product.utils';
import { AnalyticsService } from '../../core/services/analytics.service';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, AppCurrencyPipe],
  templateUrl: './checkout.component.html',
  styleUrls: ['./checkout.component.css']
})
export class CheckoutComponent implements OnInit {
  cartService = inject(CartService);
  authService = inject(AuthService);
  orderService = inject(OrderService);
  paymentMethodService = inject(PaymentMethodService);
  appSettings = inject(AppSettingsService);
  analyticsService = inject(AnalyticsService);
  router = inject(Router);
  fb = inject(FormBuilder);

  loading = signal(false);
  error = signal('');
  success = signal(false);
  createdOrderId = signal('');
  paymentMethods = signal<PaymentMethod[]>([]);
  selectedPaymentMethod = signal<PaymentMethod | null>(null);
  isAuthenticated = computed(() => this.authService.isAuthenticated());
  currentUser = computed(() => this.authService.currentUser());
  hasProfileContact = computed(() => {
    const user = this.currentUser();
    if (!user) return false;
    const hasName = Boolean((user.firstName || '').trim() || (user.lastName || '').trim());
    const hasEmail = Boolean((user.email || '').trim());
    const hasPhone = Boolean((user.phoneNumber || '').trim());
    return hasName && hasEmail && hasPhone;
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
      const base = getProductPrice(item.product);
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

  shippingForm = this.fb.group({
    fullName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phone: ['', Validators.required],
    address: ['', Validators.required],
    city: ['', Validators.required],
    zipCode: ['', Validators.required],
    country: ['', Validators.required]
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

  ngOnInit(): void {
    if (this.cartService.itemCount() === 0) {
      this.router.navigate(['/cart']);
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
        this.paymentMethods.set(methods);
        if (methods.length > 0) {
          this.selectedPaymentMethod.set(methods[0]);
        }
      },
      error: (error) => {
        console.error('Error loading payment methods:', error);
      }
    });
  }

  getPaymentTypeLabel(method: PaymentMethod): string {
    return method.typeLabel || method.type || 'Otro';
  }

  getPaymentTypeIcon(method: PaymentMethod): string {
    const label = this.getPaymentTypeLabel(method).trim();
    return label.length > 0 ? label[0].toUpperCase() : '?';
  }

  selectPaymentMethod(method: PaymentMethod): void {
    this.selectedPaymentMethod.set(method);
  }

  onSubmit(): void {
    if (this.shippingForm.invalid) {
      this.shippingForm.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set('');

    const items = this.cartService.cartItems().map(item => ({
      productId: item.product.id,
      quantity: item.quantity
    }));

    this.orderService.createOrder({ 
      items,
      shippingAddressId: undefined
    }).subscribe({
      next: (order) => {
        this.cartService.clearCart();
        this.createdOrderId.set(order.id);
        this.success.set(true);
        this.loading.set(false);
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
      },
      error: (err) => {
        console.error('Checkout error', err);
        this.error.set(err.error?.message || 'Error al procesar la orden. Intente nuevamente.');
        this.loading.set(false);
      }
    });
  }

  getItemPrice(product: Product): number {
    return getProductPrice(product);
  }

  getItemOfferPrice(product: Product): number | null {
    if (!this.isAuthenticated()) return null;
    if (typeof product.offerPrice !== 'number') return null;
    return product.offerPrice;
  }

  getItemDisplayPrice(product: Product): number {
    const base = getProductPrice(product);
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
