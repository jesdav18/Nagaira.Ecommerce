import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ProductCardComponent } from '../../../shared/components/product-card/product-card.component';
import { ProductService } from '../../../core/services/product.service';
import { Product } from '../../../core/models/models';

@Component({
  selector: 'app-home-featured',
  standalone: true,
  imports: [CommonModule, RouterLink, ProductCardComponent],
  templateUrl: './home-featured.component.html',
  styleUrls: ['./home-featured.component.css']
})
export class HomeFeaturedComponent implements OnInit {
  private readonly productLimit = 6;
  private productService = inject(ProductService);

  featuredProducts = signal<Product[]>([]);
  loading = signal(true);

  ngOnInit(): void {
    this.loadFeaturedProducts();
  }

  private loadFeaturedProducts(): void {
    this.loading.set(true);
    this.productService.getFeatured().subscribe({
      next: (products) => {
        const featured = products ?? [];
        if (featured.length >= this.productLimit) {
          this.featuredProducts.set(featured.slice(0, this.productLimit));
          this.loading.set(false);
          return;
        }

        this.productService.getAll().subscribe({
          next: (allProducts) => {
            this.featuredProducts.set(this.completeProductList(featured, allProducts));
            this.loading.set(false);
          },
          error: (error) => {
            console.error('Error loading products:', error);
            this.featuredProducts.set(featured.slice(0, this.productLimit));
            this.loading.set(false);
          }
        });
      },
      error: (error) => {
        console.error('Error loading featured products:', error);
        this.productService.getAll().subscribe({
          next: (allProducts) => {
            this.featuredProducts.set(allProducts.slice(0, this.productLimit));
            this.loading.set(false);
          },
          error: (fallbackError) => {
            console.error('Error loading products:', fallbackError);
            this.loading.set(false);
          }
        });
      }
    });
  }

  private completeProductList(featured: Product[], allProducts: Product[]): Product[] {
    const productsById = new Map<string, Product>();

    [...featured, ...allProducts].forEach(product => productsById.set(product.id, product));

    return Array.from(productsById.values()).slice(0, this.productLimit);
  }
}
