import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CategoryService } from '../../core/services/category.service';
import { ProductService } from '../../core/services/product.service';
import { Category, Product } from '../../core/models/models';
import { ProductCardComponent } from '../../shared/components/product-card/product-card.component';
import { SeoService } from '../../core/services/seo.service';
import { SeoResolveService } from '../../core/services/seo-resolve.service';
import { sortProductsByName } from '../../core/utils/product.utils';

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
  selectedCategory = signal<Category | null>(null);
  products = signal<Product[]>([]);
  loading = signal(true);
  notFound = signal(false);

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const slug = params.get('slug');
      if (slug) {
        this.loadCategory(slug);
      }
    });
  }

  private loadCategory(slug: string): void {
    this.loading.set(true);
    this.notFound.set(false);
    this.products.set([]);
    this.categoryService.getBySlug(slug).subscribe({
      next: (category) => {
        this.category.set(category);
        this.setMeta(category);
        const firstSubcategory = this.sortedSubcategories(category)[0] || null;
        const initialCategory = firstSubcategory || category;
        this.selectedCategory.set(initialCategory);
        this.loadProducts(initialCategory.id);
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
        this.products.set(sortProductsByName(products));
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading products', error);
        this.loading.set(false);
      }
    });
  }

  sortedSubcategories(category = this.category()): Category[] {
    return [...(category?.subCategories || [])]
      .filter(subcategory => subcategory.isActive)
      .sort((a, b) => a.name.localeCompare(b.name, 'es'));
  }

  selectSubcategory(subcategory: Category): void {
    this.selectedCategory.set(subcategory);
    this.loadProducts(subcategory.id);
  }

  isSelectedSubcategory(subcategory: Category): boolean {
    return this.selectedCategory()?.id === subcategory.id;
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
