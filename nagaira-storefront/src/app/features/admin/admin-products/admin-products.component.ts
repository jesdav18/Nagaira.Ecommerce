import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { AdminService } from '../../../core/services/admin.service';

@Component({
  selector: 'app-admin-products',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './admin-products.component.html',
  styleUrls: ['./admin-products.component.css']
})
export class AdminProductsComponent implements OnInit, OnDestroy {
  private adminService = inject(AdminService);
  private searchInput$ = new Subject<string>();
  private destroy$ = new Subject<void>();
  
  products = signal<any[]>([]);
  loading = signal(true);
  pageNumber = signal(1);
  pageSize = signal(20);
  totalCount = signal(0);
  totalPages = signal(0);
  searchTerm = signal('');
  isActiveFilter = signal<boolean | null>(null);
  categories = signal<any[]>([]);
  categoryFilter = signal<string | null>(null);

  ngOnInit(): void {
    this.loadMovementTypes();
    this.loadCategories();
    this.loadProducts();

    this.searchInput$
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe((value) => {
        this.searchTerm.set(value.trim());
        this.pageNumber.set(1);
        this.loadProducts();
      });
  }

  loadMovementTypes(): void {
    this.adminService.getMovementTypes().subscribe({
      next: (types: any) => {
        const entryTypes = types.filter((t: any) => t.category === 'Entrada' || t.value === 'InitialStock');
        this.movementTypes.set(Array.isArray(entryTypes) ? entryTypes : []);
      },
      error: (error) => {
        console.error('Error loading movement types:', error);
        this.movementTypes.set([
          { value: 'InitialStock', label: 'Stock Inicial' },
          { value: 'Purchase', label: 'Compra' },
          { value: 'Return', label: 'Devolución' },
          { value: 'TransferIn', label: 'Transferencia Entrada' }
        ]);
      }
    });
  }

  loadProducts(): void {
    this.loading.set(true);
    this.adminService.getProductsPaged(
      this.pageNumber(),
      this.pageSize(),
      this.searchTerm() || undefined,
      this.isActiveFilter() ?? undefined,
      this.categoryFilter() ?? undefined
    ).subscribe({
      next: (response: any) => {
        this.products.set(response.items || []);
        this.totalCount.set(response.totalCount || 0);
        this.totalPages.set(response.totalPages || 0);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading products:', error);
        this.loading.set(false);
      }
    });
  }

  onSearch(): void {
    this.searchTerm.set(this.searchTerm().trim());
    this.pageNumber.set(1);
    this.loadProducts();
  }

  onSearchInput(value: string): void {
    this.searchInput$.next(value ?? '');
  }

  onFilterChange(): void {
    this.pageNumber.set(1);
    this.loadProducts();
  }

  loadCategories(): void {
    this.adminService.getAllCategories().subscribe({
      next: (categories: any) => {
        this.categories.set(categories || []);
      },
      error: (error) => {
        console.error('Error loading categories:', error);
        this.categories.set([]);
      }
    });
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.pageNumber.set(page);
      this.loadProducts();
    }
  }

  deleteProduct(id: string): void {
    if (confirm('¿Estás seguro de eliminar este producto?')) {
      this.adminService.deleteProduct(id).subscribe({
        next: () => {
          this.loadProducts();
        },
        error: (error) => {
          console.error('Error deleting product:', error);
          alert('Error al eliminar el producto');
        }
      });
    }
  }

  toggleProductStatus(product: any): void {
    const action = product.isActive 
      ? this.adminService.deactivateProduct(product.id)
      : this.adminService.activateProduct(product.id);
    
    action.subscribe({
      next: () => {
        this.loadProducts();
      },
      error: (error: any) => {
        console.error('Error updating product:', error);
        alert('Error al actualizar el producto');
      }
    });
  }

  showMovementModal = signal(false);
  selectedProduct = signal<any>(null);
  saving = signal(false);
  
  movementTypes = signal<any[]>([]);
  
  movementForm = {
    movementType: 'InitialStock',
    quantity: 1,
    referenceNumber: '',
    notes: '',
    costPerUnit: null as number | null
  };

  openMovementModal(product: any): void {
    this.selectedProduct.set(product);
    this.resetForm();
    this.showMovementModal.set(true);
  }

  closeMovementModal(): void {
    this.showMovementModal.set(false);
    this.selectedProduct.set(null);
    this.resetForm();
  }

  resetForm(): void {
    this.movementForm = {
      movementType: 'InitialStock',
      quantity: 1,
      referenceNumber: '',
      notes: '',
      costPerUnit: null
    };
  }

  createMovement(): void {
    const product = this.selectedProduct();
    if (!product) return;

    if (this.movementForm.quantity <= 0) {
      alert('La cantidad debe ser mayor a 0');
      return;
    }

    const movement = {
      productId: product.id,
      movementType: this.movementForm.movementType,
      quantity: Math.abs(this.movementForm.quantity),
      referenceNumber: this.movementForm.referenceNumber || null,
      notes: this.movementForm.notes || null,
      costPerUnit: this.movementForm.costPerUnit || null
    };

    this.saving.set(true);
    this.adminService.createMovement(movement).subscribe({
      next: () => {
        this.saving.set(false);
        this.closeMovementModal();
        this.loadProducts();
        alert('Stock agregado correctamente');
      },
      error: (error: any) => {
        console.error('Error creating movement:', error);
        alert('Error al agregar stock: ' + (error.error?.message || error.message));
        this.saving.set(false);
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.searchInput$.complete();
  }
}

