import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink, RouterLinkActive } from '@angular/router';
import { ProductService } from '../../core/services/product.service';
import { Product, Category } from '../../core/models/models';
import { ProductCardComponent } from '../../shared/components/product-card/product-card.component';
import { CategoryService } from '../../core/services/category.service';
import { ProductRequestCtaComponent } from '../product-requests/product-request-cta.component';

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [CommonModule, RouterLink, ProductCardComponent, RouterLinkActive, ProductRequestCtaComponent],
  templateUrl: './products.component.html',
  styleUrls: ['./products.component.css']
})
export class ProductsComponent {
  private productService = inject(ProductService);
  private categoryService = inject(CategoryService);
  private route = inject(ActivatedRoute);

  products = signal<Product[]>([]);
  categories = signal<Category[]>([]);
  loading = signal(true);
  currentTitle = signal('Catálogo');
  expandedCategories = signal<Set<string>>(new Set());
  
  constructor() {
    this.route.queryParams.subscribe(params => {
      this.handleFilters(params);
      if (params['category'] && this.categories().length > 0) {
        this.expandCategoryPath(params['category'], this.categories());
      }
    });
    this.loadCategories();
  }

  private loadCategories(): void {
    this.categoryService.getAllActive().subscribe({
        next: (cats) => {
          this.categories.set(cats);
          const categoryId = this.route.snapshot.queryParams['category'];
          if (categoryId) {
            this.expandCategoryPath(categoryId, cats);
          }
        },
        error: (err) => console.error('Error loading categories', err)
    });
  }

  private expandCategoryPath(categoryId: string, categories: Category[]): void {
    const expanded = new Set(this.expandedCategories());
    
    const findAndExpand = (cats: Category[]): boolean => {
      for (const cat of cats) {
        if (cat.id === categoryId) {
          return true;
        }
        if (cat.subCategories && cat.subCategories.length > 0) {
          if (findAndExpand(cat.subCategories)) {
            expanded.add(cat.id);
            return true;
          }
        }
      }
      return false;
    };
    
    findAndExpand(categories);
    this.expandedCategories.set(expanded);
  }

  private handleFilters(params: any): void {
    const search = params['search'];
    const categoryId = params['category'];

    this.loading.set(true);

    if (search && typeof search === 'string' && search.trim().length > 0) {
      const searchTerm = search.trim();
      this.currentTitle.set(`Resultados para "${searchTerm}"`);
      this.productService.search(searchTerm).subscribe({
        next: (data) => {
          this.updateView(data);
        },
        error: (err) => {
          console.error('Error searching products:', err);
          this.loading.set(false);
        }
      });
    } else if (categoryId) {
      this.categoryService.getById(categoryId).subscribe({
        next: (category) => {
          this.currentTitle.set(category.name);
          this.productService.getByCategory(categoryId).subscribe({
            next: (data) => {
              this.updateView(data);
            },
            error: (err) => {
              console.error('Error loading products by category:', err);
              this.loading.set(false);
            }
          });
        },
        error: (err) => {
          console.error('Error loading category:', err);
          this.currentTitle.set('Categoría');
          this.productService.getByCategory(categoryId).subscribe({
            next: (data) => {
              this.updateView(data);
            },
            error: (err2) => {
              console.error('Error loading products by category:', err2);
              this.loading.set(false);
            }
          });
        }
      });
    } else {
      this.currentTitle.set('Todos los Productos');
      this.productService.getAll().subscribe({
        next: (data) => this.updateView(data),
        error: (err) => {
          console.error('Error loading all products:', err);
          this.loading.set(false);
        }
      });
    }
  }

  private updateView(data: Product[]): void {
    this.products.set(data);
    this.loading.set(false);
  }

  toggleCategory(categoryId: string, event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    const expanded = new Set(this.expandedCategories());
    if (expanded.has(categoryId)) {
      expanded.delete(categoryId);
    } else {
      expanded.add(categoryId);
    }
    this.expandedCategories.set(expanded);
  }

  isExpanded(categoryId: string): boolean {
    return this.expandedCategories().has(categoryId);
  }

  hasSubcategories(category: Category): boolean {
    return !!(category.subCategories && category.subCategories.length > 0);
  }


}
