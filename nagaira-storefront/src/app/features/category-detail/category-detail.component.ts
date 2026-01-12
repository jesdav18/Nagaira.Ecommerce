import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CategoryService } from '../../core/services/category.service';
import { ProductService } from '../../core/services/product.service';
import { Category, Product } from '../../core/models/models';
import { ProductCardComponent } from '../../shared/components/product-card/product-card.component';
import { SeoService } from '../../core/services/seo.service';
import { SeoResolveService } from '../../core/services/seo-resolve.service';

@Component({
  selector: 'app-category-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, ProductCardComponent],
  templateUrl: './category-detail.component.html',
  styleUrls: ['./category-detail.component.css']
})
export class CategoryDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private categoryService = inject(CategoryService);
  private productService = inject(ProductService);
  private seoService = inject(SeoService);
  private seoResolveService = inject(SeoResolveService);

  category = signal<Category | null>(null);
  products = signal<Product[]>([]);
  loading = signal(true);
  notFound = signal(false);

  ngOnInit(): void {
    const slug = this.route.snapshot.paramMap.get('slug');
    if (slug) {
      this.loadCategory(slug);
    }
  }

  private loadCategory(slug: string): void {
    this.loading.set(true);
    this.categoryService.getBySlug(slug).subscribe({
      next: (category) => {
        this.category.set(category);
        this.setMeta(category);
        this.loadProducts(category.id);
      },
      error: (error) => {
        console.error('Error loading category', error);
        this.resolveSlug(slug);
      }
    });
  }

  private loadProducts(categoryId: string): void {
    this.productService.getByCategory(categoryId).subscribe({
      next: (products) => {
        this.products.set(products);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading products', error);
        this.loading.set(false);
      }
    });
  }

  private setMeta(category: Category): void {
    const description = category.description?.trim()
      ? category.description.trim().slice(0, 160)
      : `Explora productos de ${category.name} en Nagaira.`;
    const image = category.imageUrl || undefined;
    const url = this.seoService.buildUrl(`/c/${category.slug}`);

    this.seoService.setMeta({
      title: `${category.name} | Nagaira`,
      description,
      image,
      url,
      type: 'website'
    });
    this.seoService.setCanonical(url);
  }

  private resolveSlug(slug: string): void {
    this.seoResolveService.resolve('category', slug).subscribe({
      next: (result) => {
        if (result.slug && result.slug !== slug) {
          window.location.replace(`/c/${result.slug}`);
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
}
