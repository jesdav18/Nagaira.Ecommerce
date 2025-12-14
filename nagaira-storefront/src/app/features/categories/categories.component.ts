import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CategoryService } from '../../core/services/category.service';
import { Category } from '../../core/models/models';

type SortOption = 'name-asc' | 'name-desc' | 'popular';
type ViewMode = 'grid' | 'list';

@Component({
  selector: 'app-categories',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './categories.component.html',
  styleUrls: ['./categories.component.css']
})
export class CategoriesComponent implements OnInit {
  private categoryService = inject(CategoryService);

  categories = signal<Category[]>([]);
  loading = signal(true);
  sortBy = signal<SortOption>('name-asc');
  viewMode = signal<ViewMode>('grid');
  expandedSubcategories = signal<Set<string>>(new Set());

  private categoryImages: Record<string, string> = {
    'tecnologia': 'https://images.unsplash.com/photo-1519389950473-47ba0277781c?q=80&w=800&auto=format&fit=crop',
    'electronic': 'https://images.unsplash.com/photo-1498049860654-af1a5c5668ba?q=80&w=800&auto=format&fit=crop',
    'moda': 'https://images.unsplash.com/photo-1445205170230-053b83016050?q=80&w=800&auto=format&fit=crop',
    'ropa': 'https://images.unsplash.com/photo-1523381210434-271e8be1f52b?q=80&w=800&auto=format&fit=crop',
    'hogar': 'https://images.unsplash.com/photo-1484101403633-562f891dc89a?q=80&w=800&auto=format&fit=crop',
    'deportes': 'https://images.unsplash.com/photo-1517649763962-0c623066013b?q=80&w=800&auto=format&fit=crop',
    'default': 'https://images.unsplash.com/photo-1604014237800-1c9102c219da?q=80&w=800&auto=format&fit=crop'
  };

  sortedCategories = computed(() => {
    const cats = [...this.categories()];
    const sort = this.sortBy();
    
    switch (sort) {
      case 'name-asc':
        return cats.sort((a, b) => a.name.localeCompare(b.name));
      case 'name-desc':
        return cats.sort((a, b) => b.name.localeCompare(a.name));
      case 'popular':
        return cats;
      default:
        return cats;
    }
  });

  ngOnInit() {
    this.categoryService.getAll().subscribe({
      next: (data) => {
        this.categories.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error loading categories', err);
        this.loading.set(false);
      }
    });
  }

  getImage(cat: Category): string {
    if (cat.imageUrl) {
      return cat.imageUrl;
    }
    const slug = cat.slug.toLowerCase();
    const match = Object.keys(this.categoryImages).find(key => slug.includes(key));
    return match ? this.categoryImages[match] : this.categoryImages['default'];
  }

  getVisibleSubcategories(cat: Category): Category[] {
    if (!cat.subCategories || cat.subCategories.length === 0) return [];
    return cat.subCategories.slice(0, 6);
  }

  hasMoreSubcategories(cat: Category): boolean {
    return cat.subCategories ? cat.subCategories.length > 6 : false;
  }

  getHiddenSubcategoriesCount(cat: Category): number {
    if (!cat.subCategories) return 0;
    return Math.max(0, cat.subCategories.length - 6);
  }

  toggleSubcategories(categoryId: string, event: Event): void {
    event.stopPropagation();
    const expanded = new Set(this.expandedSubcategories());
    if (expanded.has(categoryId)) {
      expanded.delete(categoryId);
    } else {
      expanded.add(categoryId);
    }
    this.expandedSubcategories.set(expanded);
  }

  isSubcategoriesExpanded(categoryId: string): boolean {
    return this.expandedSubcategories().has(categoryId);
  }

  getAllSubcategories(cat: Category): Category[] {
    return cat.subCategories || [];
  }

  getViewMoreText(cat: Category): string {
    if (this.isSubcategoriesExpanded(cat.id)) {
      return 'Ver menos';
    }
    const count = this.getHiddenSubcategoriesCount(cat);
    return `+${count} más`;
  }

  onSortChange(sort: SortOption): void {
    this.sortBy.set(sort);
  }

  toggleViewMode(): void {
    this.viewMode.set(this.viewMode() === 'grid' ? 'list' : 'grid');
  }

  navigateToCategory(categoryId: string, event: Event): void {
    const target = event.target as HTMLElement;
    if (target.closest('.category-actions') || target.closest('.subcategory-chip') || target.closest('.view-more-chip')) {
      event.preventDefault();
      return;
    }
  }
}
