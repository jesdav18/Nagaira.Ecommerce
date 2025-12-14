import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ProductService } from '../../core/services/product.service';
import { CartService } from '../../core/services/cart.service';
import { AppCurrencyPipe } from '../../core/pipes/currency.pipe';
import { Product } from '../../core/models/models';
import { getProductPrice, getProductStock } from '../../core/utils/product.utils';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, AppCurrencyPipe],
  templateUrl: './product-detail.component.html',
  styleUrls: ['./product-detail.component.css']
})
export class ProductDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private productService = inject(ProductService);
  private cartService = inject(CartService);

  product = signal<Product | null>(null);
  loading = signal(true);
  selectedImageIndex = signal(0);
  quantity = signal(1);
  addingToCart = signal(false);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadProduct(id);
    }
  }

  loadProduct(id: string): void {
    this.productService.getById(id).subscribe({
      next: (product) => {
        this.product.set(product);
        const primaryIndex = product.images.findIndex(img => img.isPrimary);
        if (primaryIndex !== -1) this.selectedImageIndex.set(primaryIndex);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading product', error);
        this.loading.set(false);
      }
    });
  }

  get finalPrice(): number {
    const p = this.product();
    return p ? getProductPrice(p) : 0;
  }

  get discountPercentage(): number {
    return 0;
  }

  get stock(): number {
    const p = this.product();
    return p ? getProductStock(p) : 0;
  }

  incrementQuantity(): void {
    const p = this.product();
    if (p && this.quantity() < this.stock) {
      this.quantity.update(q => q + 1);
    }
  }

  decrementQuantity(): void {
    if (this.quantity() > 1) {
      this.quantity.update(q => q - 1);
    }
  }

  addToCart(): void {
    const p = this.product();
    if (p) {
      this.addingToCart.set(true);
      this.cartService.addToCart(p, this.quantity());
      
      setTimeout(() => {
        this.addingToCart.set(false);
      }, 1000);
    }
  }
}
