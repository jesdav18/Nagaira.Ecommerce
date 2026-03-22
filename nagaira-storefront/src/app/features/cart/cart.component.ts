import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CartService } from '../../core/services/cart.service';
import { AuthService } from '../../core/services/auth.service';
import { AppSettingsService } from '../../core/services/app-settings.service';
import { AppCurrencyPipe } from '../../core/pipes/currency.pipe';
import { CreateQuoteRequest, Product, Quote } from '../../core/models/models';
import { getProductPriceByQuantity, getProductStock, isVirtualStock } from '../../core/utils/product.utils';
import { NotificationService } from '../../core/services/notification.service';
import { QuoteService } from '../../core/services/quote.service';
import Swal from 'sweetalert2';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, RouterLink, AppCurrencyPipe],
  templateUrl: './cart.component.html',
  styleUrls: ['./cart.component.css']
})
export class CartComponent {
  cartService = inject(CartService);
  private appSettings = inject(AppSettingsService);
  private authService = inject(AuthService);
  private notificationService = inject(NotificationService);
  private quoteService = inject(QuoteService);
  generatingQuote = signal(false);
  includeShippingInQuote = signal(false);
  quoteShippingAmount = signal(0);

  updateQuantity(productId: string, quantity: number): void {
    this.cartService.updateQuantity(productId, quantity);
  }

  async removeItem(productId: string): Promise<void> {
    const confirmed = await this.notificationService.confirm('Estas seguro de eliminar este producto?');
    if (confirmed) {
      this.cartService.removeFromCart(productId);
    }
  }

  get finalTotal(): number {
    return this.cartService.total();
  }

  get discountTotal(): number {
    if (!this.authService.isAuthenticated()) return 0;
    return this.cartService.cartItems().reduce((total, item) => {
      const base = getProductPriceByQuantity(item.product, item.quantity);
      const offer = this.getItemOfferPrice(item.product);
      if (offer === null || offer >= base) return total;
      return total + ((base - offer) * item.quantity);
    }, 0);
  }

  get discountedTotal(): number {
    return Math.max(this.cartService.total() - this.discountTotal, 0);
  }

  get discountedSubtotal(): number {
    const rate = this.appSettings.taxRate();
    const divisor = 1 + rate;
    if (divisor <= 0) return this.discountedTotal;
    return this.discountedTotal / divisor;
  }

  get discountedTax(): number {
    return this.discountedTotal - this.discountedSubtotal;
  }

  get quoteShippingTotal(): number {
    if (!this.includeShippingInQuote()) {
      return 0;
    }

    const amount = this.quoteShippingAmount();
    return amount > 0 ? amount : 0;
  }

  get quoteGrandTotal(): number {
    return this.discountedTotal + this.quoteShippingTotal;
  }

  getItemPrice(product: Product, quantity: number): number {
    return getProductPriceByQuantity(product, quantity);
  }

  getItemOfferPrice(product: Product): number | null {
    if (!this.authService.isAuthenticated()) return null;
    if (typeof product.offerPrice !== 'number') return null;
    return product.offerPrice;
  }

  getItemDisplayPrice(product: Product, quantity: number): number {
    const base = getProductPriceByQuantity(product, quantity);
    const offer = this.getItemOfferPrice(product);
    if (offer !== null && offer < base) return offer;
    return base;
  }

  getItemStock(product: Product): number | null {
    return getProductStock(product);
  }

  isItemVirtualStock(product: Product): boolean {
    return isVirtualStock(product);
  }

  canIncrementQuantity(product: Product, currentQuantity: number): boolean {
    if (this.isItemVirtualStock(product)) {
      return true;
    }
    const stock = this.getItemStock(product);
    return stock !== null && currentQuantity < stock;
  }

  onShippingToggle(enabled: boolean): void {
    this.includeShippingInQuote.set(enabled);
    if (!enabled) {
      this.quoteShippingAmount.set(0);
    }
  }

  updateQuoteShippingAmount(rawValue: string): void {
    const parsed = Number(rawValue);
    if (!Number.isFinite(parsed) || parsed <= 0) {
      this.quoteShippingAmount.set(0);
      return;
    }

    this.quoteShippingAmount.set(Math.round(parsed * 100) / 100);
  }

  async generateQuotePdf(): Promise<void> {
    if (this.generatingQuote()) {
      return;
    }
    const items = this.cartService.cartItems();
    if (items.length === 0) {
      this.notificationService.warning('Agrega productos al carrito para generar una cotizacion.');
      return;
    }
    this.generatingQuote.set(true);

    try {
      const shippingAmount = this.quoteShippingTotal;
      const customer = await this.resolveQuoteCustomer();
      if (!customer) {
        return;
      }

    const quoteItems = items.map(item => {
      const unitPrice = this.getItemDisplayPrice(item.product, item.quantity);
      const unitPriceOriginal = this.getItemPrice(item.product, item.quantity);
      const lineTotal = unitPrice * item.quantity;
      return {
        productId: item.product.id,
        productName: item.product.name,
        sku: item.product.sku || '',
        quantity: item.quantity,
        unitPrice,
        unitPriceOriginal: unitPriceOriginal > unitPrice ? unitPriceOriginal : null,
        subtotal: lineTotal
      };
    });

    const payload: CreateQuoteRequest = {
      customerName: customer.customerName,
      customerTaxId: customer.customerTaxId,
      customerType: customer.customerType,
      currencySymbol: this.appSettings.getCurrencySymbol(),
      taxLabel: this.appSettings.getTaxLabel(),
      taxRate: this.appSettings.getTaxRate(),
      subtotal: this.discountedSubtotal,
      tax: this.discountedTax,
      shippingAmount,
      discount: this.discountTotal,
      total: this.discountedTotal + shippingAmount,
      items: quoteItems
    };

      let persistedQuote: Quote;
      try {
        persistedQuote = await firstValueFrom(this.quoteService.createQuote(payload));
      } catch (error: any) {
        console.error('Error saving quote', error);
        this.notificationService.error(error?.error?.message || 'No se pudo guardar la cotizacion en la base de datos.');
        return;
      }

    const rows = quoteItems.map((item, index) => {
      const lineTotal = item.subtotal;
      return `
        <tr>
          <td>${index + 1}</td>
          <td>${this.escapeHtml(item.productName)}</td>
          <td>${this.escapeHtml(item.sku || '-')}</td>
          <td class="text-right">${item.quantity}</td>
          <td class="text-right">${this.formatCurrency(item.unitPrice)}</td>
          <td class="text-right">${this.formatCurrency(lineTotal)}</td>
        </tr>
      `;
    });

      const quoteNumber = persistedQuote.quoteNumber || persistedQuote.id;
      const generatedAt = new Date(persistedQuote.createdAt || new Date().toISOString()).toLocaleString('es-HN');
      const logoUrl = this.toAbsoluteUrl('/assets/images/NagairaLogoNombre.png');
      const discountSection = this.discountTotal > 0
        ? `<div class="summary-row"><span>Descuento</span><span>-${this.formatCurrency(this.discountTotal)}</span></div>`
        : '';
      const shippingSection = shippingAmount > 0
        ? `<div class="summary-row"><span>Envio</span><span>${this.formatCurrency(shippingAmount)}</span></div>`
        : '';
    const taxIdSection = customer.customerTaxId
      ? `<strong>RTN:</strong> ${this.escapeHtml(customer.customerTaxId)}`
      : '<strong>RTN:</strong> Consumidor final';
      const html = `<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8" />
  <title>Cotizacion ${quoteNumber}</title>
  <style>
    @page { size: A4; margin: 16mm; }
    * { box-sizing: border-box; }
    body { font-family: Arial, sans-serif; color: #111827; margin: 0; }
    h1 { margin: 0; font-size: 22px; }
    .header { display: flex; justify-content: space-between; gap: 16px; margin-bottom: 18px; }
    .brand { display: flex; justify-content: flex-end; align-items: center; width: 100%; }
    .brand-logo { height: 44px; width: auto; }
    .brand-name { font-size: 16px; font-weight: 800; }
    .muted { color: #6b7280; font-size: 12px; }
    .customer { margin: 14px 0; padding: 10px; border: 1px solid #e5e7eb; border-radius: 8px; font-size: 13px; }
    table { width: 100%; border-collapse: collapse; margin-top: 12px; }
    th, td { border-bottom: 1px solid #e5e7eb; padding: 8px 6px; font-size: 12px; vertical-align: top; }
    th { text-align: left; background: #f9fafb; font-weight: 700; }
    .text-right { text-align: right; }
    .summary { margin-top: 16px; margin-left: auto; width: 320px; border: 1px solid #e5e7eb; border-radius: 8px; padding: 10px; }
    .summary-row { display: flex; justify-content: space-between; padding: 4px 0; font-size: 13px; }
    .summary-total { font-size: 16px; font-weight: 800; border-top: 1px solid #e5e7eb; margin-top: 6px; padding-top: 8px; }
    .note { margin-top: 16px; color: #6b7280; font-size: 12px; }
  </style>
</head>
<body>
  <div class="header">
    <div>
      <h1>Cotizacion</h1>
      <div class="muted">No. ${quoteNumber}</div>
      <div class="muted">Generado: ${generatedAt}</div>
    </div>
    <div style="text-align:right;">
      <div class="brand">
        <img class="brand-logo" src="${this.escapeHtml(logoUrl)}" alt="Nagaira" />
      </div>
      <div class="muted">hello@nagaira.com</div>
      <div class="muted">+504 9201-6464</div>
    </div>
  </div>

  <div class="customer">
    <strong>Cliente:</strong> ${this.escapeHtml(customer.customerName)}<br/>
    ${taxIdSection}
  </div>

  <table>
    <thead>
      <tr>
        <th>#</th>
        <th>Producto</th>
        <th>SKU</th>
        <th class="text-right">Cant.</th>
        <th class="text-right">Precio unit.</th>
        <th class="text-right">Subtotal</th>
      </tr>
    </thead>
    <tbody>
      ${rows.join('')}
    </tbody>
  </table>

  <div class="summary">
    <div class="summary-row"><span>Subtotal</span><span>${this.formatCurrency(persistedQuote.subtotal)}</span></div>
    ${discountSection}
    <div class="summary-row"><span>${this.escapeHtml(this.appSettings.getTaxLabel())}</span><span>${this.formatCurrency(persistedQuote.tax)}</span></div>
    ${shippingSection}
    <div class="summary-row summary-total"><span>Total</span><span>${this.formatCurrency(persistedQuote.total)}</span></div>
  </div>

  <div class="note">Cotizacion sujeta a cambios de precio y disponibilidad.</div>
  <script>window.onload = () => setTimeout(() => window.print(), 250);</script>
</body>
</html>`;

      this.printHtmlInIframe(html);
    } finally {
      this.generatingQuote.set(false);
    }
  }

  private formatCurrency(amount: number): string {
    return `${this.appSettings.getCurrencySymbol()}${amount.toFixed(2)}`;
  }

  private escapeHtml(text: string): string {
    return text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  private toAbsoluteUrl(url: string): string {
    if (!url) return url;
    if (url.startsWith('http://') || url.startsWith('https://') || url.startsWith('data:')) {
      return url;
    }
    const origin = window.location.origin;
    if (url.startsWith('/')) {
      return `${origin}${url}`;
    }
    return `${origin}/${url}`;
  }

  private async resolveQuoteCustomer(): Promise<{ customerName: string; customerTaxId: string | null; customerType: 'named' | 'consumer_final' } | null> {
    const user = this.authService.currentUser();
    const userName = user ? `${user.firstName ?? ''} ${user.lastName ?? ''}`.trim() : '';
    const userTaxId = user?.taxId?.trim() || '';
    const hasDefinedCustomer = this.authService.isAuthenticated() && userName.length > 0;

    if (hasDefinedCustomer) {
      if (userTaxId) {
        return {
          customerName: userName,
          customerTaxId: userTaxId,
          customerType: 'named'
        };
      }

      const result = await Swal.fire({
        title: 'RTN para cotizacion',
        html: `
          <div style="text-align:left;font-size:14px;margin:0 0 8px 0;">Completa los datos para generar la cotizacion con nombre.</div>
          <label for="quote-customer-name" style="display:block;text-align:left;font-size:13px;font-weight:600;margin:10px 0 4px;">Nombre</label>
          <input id="quote-customer-name" class="swal2-input" placeholder="Ejemplo: Juan Perez" value="${this.escapeHtml(userName)}" style="margin:0;width:100%;" />
          <label for="quote-customer-taxid" style="display:block;text-align:left;font-size:13px;font-weight:600;margin:10px 0 4px;">RTN</label>
          <input id="quote-customer-taxid" class="swal2-input" placeholder="Ejemplo: 08011999123456" style="margin:0;width:100%;" />
        `,
        focusConfirm: false,
        showCancelButton: true,
        showDenyButton: true,
        confirmButtonText: 'Guardar con nombre',
        denyButtonText: 'Consumidor final',
        cancelButtonText: 'Cancelar',
        preConfirm: () => {
          const nameInput = document.getElementById('quote-customer-name') as HTMLInputElement | null;
          const taxIdInput = document.getElementById('quote-customer-taxid') as HTMLInputElement | null;
          const name = nameInput?.value?.trim() || '';
          const taxId = taxIdInput?.value?.trim() || '';
          if (!name || !taxId) {
            Swal.showValidationMessage('Nombre y RTN son obligatorios');
            return null;
          }
          return { name, taxId };
        }
      });

      if (result.isDenied) {
        return {
          customerName: 'Consumidor final',
          customerTaxId: null,
          customerType: 'consumer_final'
        };
      }

      if (!result.isConfirmed || !result.value) {
        return null;
      }

      return {
        customerName: result.value.name,
        customerTaxId: result.value.taxId,
        customerType: 'named'
      };
    }

    const shouldUseNameAndTaxId = await this.notificationService.confirm(
      'Deseas generar la cotizacion con nombre y RTN?'
    );

    if (!shouldUseNameAndTaxId) {
      return {
        customerName: 'Consumidor final',
        customerTaxId: null,
        customerType: 'consumer_final'
      };
    }

    const result = await Swal.fire({
      title: 'Datos para cotizacion',
      html: `
        <div style="text-align:left;font-size:14px;margin:0 0 8px 0;">Ingresa los datos del cliente para la cotizacion.</div>
        <label for="quote-customer-name" style="display:block;text-align:left;font-size:13px;font-weight:600;margin:10px 0 4px;">Nombre</label>
        <input id="quote-customer-name" class="swal2-input" placeholder="Ejemplo: Juan Perez" style="margin:0;width:100%;" />
        <label for="quote-customer-taxid" style="display:block;text-align:left;font-size:13px;font-weight:600;margin:10px 0 4px;">RTN</label>
        <input id="quote-customer-taxid" class="swal2-input" placeholder="Ejemplo: 08011999123456" style="margin:0;width:100%;" />
      `,
      focusConfirm: false,
      showCancelButton: true,
      confirmButtonText: 'Guardar',
      cancelButtonText: 'Cancelar',
      preConfirm: () => {
        const nameInput = document.getElementById('quote-customer-name') as HTMLInputElement | null;
        const taxIdInput = document.getElementById('quote-customer-taxid') as HTMLInputElement | null;
        const name = nameInput?.value?.trim() || '';
        const taxId = taxIdInput?.value?.trim() || '';
        if (!name || !taxId) {
          Swal.showValidationMessage('Nombre y RTN son obligatorios');
          return null;
        }
        return { name, taxId };
      }
    });

    if (!result.isConfirmed || !result.value) {
      return null;
    }

    return {
      customerName: result.value.name,
      customerTaxId: result.value.taxId,
      customerType: 'named'
    };
  }

  private printHtmlInIframe(html: string): void {
    const iframe = document.createElement('iframe');
    iframe.style.position = 'fixed';
    iframe.style.right = '0';
    iframe.style.bottom = '0';
    iframe.style.width = '0';
    iframe.style.height = '0';
    iframe.style.border = '0';
    document.body.appendChild(iframe);

    const doc = iframe.contentWindow?.document;
    if (!doc) {
      this.notificationService.error('No se pudo generar la cotizacion.');
      document.body.removeChild(iframe);
      return;
    }

    doc.open();
    doc.write(html);
    doc.close();

    const win = iframe.contentWindow;
    if (!win) {
      this.notificationService.error('No se pudo generar la cotizacion.');
      document.body.removeChild(iframe);
      return;
    }

    win.onafterprint = () => {
      window.setTimeout(() => {
        if (iframe.parentNode) {
          iframe.parentNode.removeChild(iframe);
        }
      }, 0);
    };

    window.setTimeout(() => {
      win.focus();
      win.print();
    }, 300);
  }
}
