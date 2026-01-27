import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CartService } from '../../core/services/cart.service';
import { AuthService } from '../../core/services/auth.service';
import { AppSettingsService } from '../../core/services/app-settings.service';
import { AppCurrencyPipe } from '../../core/pipes/currency.pipe';
import { Product } from '../../core/models/models';
import { getProductPrice, getProductStock, isVirtualStock } from '../../core/utils/product.utils';
import { NotificationService } from '../../core/services/notification.service';

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
      const base = getProductPrice(item.product);
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

  getItemPrice(product: Product): number {
    return getProductPrice(product);
  }

  getItemOfferPrice(product: Product): number | null {
    if (!this.authService.isAuthenticated()) return null;
    if (typeof product.offerPrice !== 'number') return null;
    return product.offerPrice;
  }

  getItemDisplayPrice(product: Product): number {
    const base = getProductPrice(product);
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
}
