import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Product } from '../../../core/models/models';
import { CartService } from '../../../core/services/cart.service';
import { AppCurrencyPipe } from '../../../core/pipes/currency.pipe';
import { getProductPrice, getProductStock, getPrimaryImage, isVirtualStock } from '../../../core/utils/product.utils';

@Component({
  selector: 'app-product-card',
  standalone: true,
  imports: [CommonModule, RouterLink, AppCurrencyPipe],
  templateUrl: './product-card.component.html',
  styleUrls: ['./product-card.component.css']
})
export class ProductCardComponent {
  @Input({ required: true }) product!: Product;

  cartService = inject(CartService);
  addingToCart = false;

  get finalPrice(): number {
    return getProductPrice(this.product);
  }

  get hasDiscount(): boolean {
    return false;
  }

  get discountPercentage(): number {
    return 0;
  }

  get primaryImage(): string {
    return getPrimaryImage(this.product);
  }

  get stock(): number | null {
    return getProductStock(this.product);
  }

  get isVirtual(): boolean {
    return isVirtualStock(this.product);
  }

  addToCart(event: Event): void {
    event.preventDefault();
    event.stopPropagation();

    this.addingToCart = true;
    this.cartService.addToCart(this.product, 1);

    setTimeout(() => {
      this.addingToCart = false;
    }, 1000);
  }
}
