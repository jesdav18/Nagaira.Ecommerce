import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';

@Component({
  selector: 'app-admin-categories',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './admin-categories.component.html',
  styleUrls: ['./admin-categories.component.css']
})
export class AdminCategoriesComponent implements OnInit {
  private adminService = inject(AdminService);
  
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

  deleteCategory(id: string): void {
    if (confirm('¿Estás seguro de eliminar esta categoría?')) {
      this.adminService.deleteCategory(id).subscribe({
        next: () => {
          this.loadCategories();
        },
        error: (error) => {
          console.error('Error deleting category:', error);
          alert('Error al eliminar la categoría');
        }
      });
    }
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
        alert('Error al actualizar la categoría');
      }
    });
  }

  getParentCategoryName(categoryId: string | null): string {
    if (!categoryId) return '-';
    const parent = this.categories().find(c => c.id === categoryId);
    return parent ? parent.name : '-';
  }
}

