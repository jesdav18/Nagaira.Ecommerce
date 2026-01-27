import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ProductService } from '../../core/services/product.service';
import { CartService } from '../../core/services/cart.service';
import { AppCurrencyPipe } from '../../core/pipes/currency.pipe';
import { Product } from '../../core/models/models';
import { getProductPrice, getProductStock, isVirtualStock } from '../../core/utils/product.utils';
import { SeoService } from '../../core/services/seo.service';
import { SeoResolveService } from '../../core/services/seo-resolve.service';
import { AnalyticsService } from '../../core/services/analytics.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, AppCurrencyPipe],
  templateUrl: './product-detail.component.html',
  styleUrls: ['./product-detail.component.css']
})
export class ProductDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private productService = inject(ProductService);
  private cartService = inject(CartService);
  private seoService = inject(SeoService);
  private seoResolveService = inject(SeoResolveService);
  private analyticsService = inject(AnalyticsService);
  private authService = inject(AuthService);

  product = signal<Product | null>(null);
  loading = signal(true);
  notFound = signal(false);
  selectedImageIndex = signal(0);
  quantity = signal(1);
  addingToCart = signal(false);

  ngOnInit(): void {
    const slug = this.route.snapshot.paramMap.get('slug');
    const id = this.route.snapshot.paramMap.get('id');
    if (slug) {
      this.loadProductBySlug(slug);
      return;
    }
    if (id) {
      this.loadProductById(id);
    }
  }

  loadProductBySlug(slug: string): void {
    this.productService.getBySlug(slug).subscribe({
      next: (product) => {
        this.product.set(product);
        this.setMeta(product);
        const primaryIndex = product.images.findIndex(img => img.isPrimary);
        if (primaryIndex !== -1) this.selectedImageIndex.set(primaryIndex);
        this.analyticsService.viewProduct({ id: product.id, name: product.name, price: getProductPrice(product) });
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading product', error);
        this.resolveSlug(slug);
      }
    });
  }

  loadProductById(id: string): void {
    this.productService.getById(id).subscribe({
      next: (product) => {
        this.product.set(product);
        this.setMeta(product);
        const primaryIndex = product.images.findIndex(img => img.isPrimary);
        if (primaryIndex !== -1) this.selectedImageIndex.set(primaryIndex);
        this.analyticsService.viewProduct({ id: product.id, name: product.name, price: getProductPrice(product) });
        this.loading.set(false);
        if (product.slug) {
          this.router.navigate(['/p', product.slug], { replaceUrl: true });
        }
      },
      error: (error) => {
        console.error('Error loading product', error);
        this.notFound.set(true);
        this.loading.set(false);
      }
    });
  }

  private resolveSlug(slug: string): void {
    this.seoResolveService.resolve('product', slug).subscribe({
      next: (result) => {
        if (result.slug && result.slug !== slug) {
          this.router.navigate(['/p', result.slug], { replaceUrl: true });
          return;
        }
        this.notFound.set(true);
        this.loading.set(false);
      },
      error: () => {
        this.notFound.set(true);
        this.loading.set(false);
      }
    });
  }

  private setMeta(product: Product): void {
    const description = product.description?.trim()
      ? product.description.trim().slice(0, 160)
      : `Compra ${product.name} en Nagaira.`;
    const image = product.images.find(img => img.isPrimary)?.imageUrl || product.images[0]?.imageUrl;
    const url = this.seoService.buildUrl(`/p/${product.slug}`);

    this.seoService.setMeta({
      title: `${product.name} | Nagaira`,
      description,
      image,
      url,
      type: 'product'
    });
    this.seoService.setCanonical(url);
  }

  get basePrice(): number {
    const p = this.product();
    return p ? getProductPrice(p) : 0;
  }

  get offerPrice(): number | null {
    const p = this.product();
    if (!p) return null;
    return typeof p.offerPrice === 'number' ? p.offerPrice : null;
  }

  get showOffer(): boolean {
    if (!this.authService.isAuthenticated()) return false;
    if (this.offerPrice === null) return false;
    return this.offerPrice < this.basePrice;
  }

  get finalPrice(): number {
    return this.showOffer && this.offerPrice !== null ? this.offerPrice : this.basePrice;
  }

  get discountPercentage(): number {
    if (!this.showOffer || this.offerPrice === null || this.basePrice <= 0) return 0;
    return Math.round(((this.basePrice - this.offerPrice) / this.basePrice) * 100);
  }

  get stock(): number | null {
    const p = this.product();
    return p ? getProductStock(p) : null;
  }

  get isVirtual(): boolean {
    const p = this.product();
    return p ? isVirtualStock(p) : false;
  }

  incrementQuantity(): void {
    const p = this.product();
    if (p) {
      if (this.isVirtual) {
        this.quantity.update(q => q + 1);
      } else if (this.stock !== null && this.quantity() < this.stock) {
        this.quantity.update(q => q + 1);
      }
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
