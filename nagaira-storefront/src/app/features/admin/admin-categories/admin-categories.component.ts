import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-admin-categories',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './admin-categories.component.html',
  styleUrls: ['./admin-categories.component.css']
})
export class AdminCategoriesComponent implements OnInit {
  private adminService = inject(AdminService);
  private notificationService = inject(NotificationService);
  
  categories = signal<any[]>([]);
  loading = signal(true);
  searchTerm = signal('');
  isActiveFilter = signal<boolean | null>(null);

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    this.loading.set(true);
    this.adminService.getAllCategories().subscribe({
      next: (categories: any) => {
        let filtered = Array.isArray(categories) ? categories : [];
        
        if (this.isActiveFilter() !== null) {
          filtered = filtered.filter(cat => cat.isActive === this.isActiveFilter());
        }
        
        this.categories.set(filtered);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading categories:', error);
        this.loading.set(false);
      }
    });
  }

  onSearch(): void {
    this.loadCategories();
  }

  onFilterChange(): void {
    this.loadCategories();
  }

  filteredCategories(): any[] {
    const term = this.searchTerm().toLowerCase();
    if (!term) return this.categories();
    
    return this.categories().filter(cat => 
      cat.name.toLowerCase().includes(term) ||
      cat.description?.toLowerCase().includes(term)
    );
  }

  async deleteCategory(id: string): Promise<void> {
    const confirmed = await this.notificationService.confirm('Estas seguro de eliminar esta categoria?');
    if (!confirmed) return;

    this.adminService.deleteCategory(id).subscribe({
      next: () => {
        this.loadCategories();
      },
      error: (error) => {
        console.error('Error deleting category:', error);
        this.notificationService.error('Error al eliminar la categoria');
      }
    });
  }

  toggleCategoryStatus(category: any): void {
    const action = category.isActive 
      ? this.adminService.deactivateCategory(category.id)
      : this.adminService.activateCategory(category.id);
    
    action.subscribe({
      next: () => {
        this.loadCategories();
      },
      error: (error) => {
        console.error('Error updating category:', error);
        this.notificationService.error('Error al actualizar la categoria');
      }
    });
  }

  async setCategoryFeaturedProducts(category: any, isFeatured: boolean): Promise<void> {
    const actionLabel = isFeatured ? 'destacar' : 'quitar de destacados';
    const confirmed = await this.notificationService.confirm(
      `Estas seguro de ${actionLabel} todos los productos de "${category.name}"?`
    );
    if (!confirmed) return;

    this.adminService.setCategoryFeaturedProducts(category.id, isFeatured).subscribe({
      next: (response: any) => {
        const count = response?.updatedCount ?? 0;
        const message = isFeatured
          ? `Productos destacados actualizados (${count})`
          : `Productos destacados removidos (${count})`;
        this.notificationService.success(message);
      },
      error: (error) => {
        console.error('Error updating featured products for category:', error);
        this.notificationService.error('Error al actualizar destacados de la categoria');
      }
    });
  }

  getParentCategoryName(categoryId: string | null): string {
    if (!categoryId) return '-';
    const parent = this.categories().find(c => c.id === categoryId);
    return parent ? parent.name : '-';
  }
}

