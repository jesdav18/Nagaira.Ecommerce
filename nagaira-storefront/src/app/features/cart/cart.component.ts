import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CartService } from '../../core/services/cart.service';
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

  getItemPrice(product: Product): number {
    return getProductPrice(product);
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
