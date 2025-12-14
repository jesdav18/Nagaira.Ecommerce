import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ProductCardComponent } from '../../shared/components/product-card/product-card.component';
import { ProductService } from '../../core/services/product.service';
import { Product } from '../../core/models/models';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink, ProductCardComponent],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  productService = inject(ProductService);
  
  featuredProducts = signal<Product[]>([]);
  loading = signal(true);

  ngOnInit(): void {
    this.loadFeaturedProducts();
  }

  loadFeaturedProducts(): void {
    this.productService.getAll().subscribe({
      next: (products) => {
        this.featuredProducts.set(products.slice(0, 8));
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading products:', error);
        this.loading.set(false);
      }
    });
  }
}
