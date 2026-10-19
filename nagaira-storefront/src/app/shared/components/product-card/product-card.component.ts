import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Product } from '../../../core/models/models';
import { CartService } from '../../../core/services/cart.service';
import { AppCurrencyPipe } from '../../../core/pipes/currency.pipe';
import {
  getProductOfferPrice,
  getProductPrice,
  getProductStock,
  getPrimaryImage,
  isVirtualStock,
  shouldShowOffer
} from '../../../core/utils/product.utils';

@Component({
  selector: 'app-product-card',
  standalone: true,
  imports: [CommonModule, RouterLink, AppCurrencyPipe],
  templateUrl: './product-card.component.html',
  styleUrls: ['./product-card.component.css']
})
export class ProductCardComponent {
  @Input({ required: true }) product!: Product;
  @Input() layout: 'default' | 'wide' = 'default';

  cartService = inject(CartService);
  addingToCart = false;

  get basePrice(): number {
    return getProductPrice(this.product);
  }

  get offerPrice(): number | null {
    return getProductOfferPrice(this.product);
  }

  get showOffer(): boolean {
    return shouldShowOffer(this.product);
  }

  get finalPrice(): number {
    return this.showOffer && this.offerPrice !== null ? this.offerPrice : this.basePrice;
  }

  get hasDiscount(): boolean {
    return this.showOffer;
  }

  get discountPercentage(): number {
    if (!this.showOffer || this.offerPrice === null || this.basePrice <= 0) return 0;
    return Math.round(((this.basePrice - this.offerPrice) / this.basePrice) * 100);
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
