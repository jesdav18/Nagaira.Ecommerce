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
        if (products && products.length > 0) {
          this.featuredProducts.set(products.slice(0, 8));
          this.loading.set(false);
          return;
        }
        this.productService.getAll().subscribe({
          next: (allProducts) => {
            this.featuredProducts.set(allProducts.slice(0, 8));
            this.loading.set(false);
          },
          error: (error) => {
            console.error('Error loading products:', error);
            this.loading.set(false);
          }
        });
      },
      error: (error) => {
        console.error('Error loading featured products:', error);
        this.productService.getAll().subscribe({
          next: (allProducts) => {
            this.featuredProducts.set(allProducts.slice(0, 8));
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
}
