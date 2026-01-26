import { Component, OnInit, OnDestroy, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { AdminService } from '../../../core/services/admin.service';
import { AppCurrencyPipe } from '../../../core/pipes/currency.pipe';
import { PriceLevel, Product, ProductPrice } from '../../../core/models/models';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-admin-products',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, AppCurrencyPipe],
  templateUrl: './admin-products.component.html',
  styleUrls: ['./admin-products.component.css']
})
export class AdminProductsComponent implements OnInit, OnDestroy {
  private adminService = inject(AdminService);
  private notificationService = inject(NotificationService);
  private searchInput$ = new Subject<string>();
  private destroy$ = new Subject<void>();
  
  products = signal<Product[]>([]);
  priceLevels = signal<PriceLevel[]>([]);
  loading = signal(true);
  pageNumber = signal(1);
  pageSize = signal(20);
  totalCount = signal(0);
  totalPages = signal(0);
  searchTerm = signal('');
  isActiveFilter = signal<boolean | null>(null);
  isFeaturedFilter = signal<boolean | null>(null);
  categories = signal<any[]>([]);
  categoryFilter = signal<string | null>(null);
  showPriceLevels = signal(false);
  priceLevelFilter = signal<string | null>(null);

  activePriceLevelIds = computed(() => {
    return new Set(this.priceLevels().filter(level => level.isActive).map(level => level.id));
  });

  activePriceLevelCount = computed(() => this.activePriceLevelIds().size);

  filteredProducts = computed(() => {
    const filter = this.priceLevelFilter();
    const items = this.products();
    if (!filter) return items;

    const activeLevelsCount = this.activePriceLevelCount();
    if (filter === 'all' && activeLevelsCount === 0) return items;

    return items.filter(product => {
      const count = this.getProductActivePriceLevelCount(product);
      switch (filter) {
        case 'none':
          return count === 0;
        case 'one':
          return count === 1;
        case 'two':
          return count === 2;
        case 'three':
          return count === 3;
        case 'all':
          return count === activeLevelsCount;
        default:
          return true;
      }
    });
  });

  ngOnInit(): void {
    this.loadMovementTypes();
    this.loadCategories();
    this.loadPriceLevels();
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
      this.categoryFilter() ?? undefined,
      this.isFeaturedFilter() ?? undefined
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

  loadPriceLevels(): void {
    this.adminService.getAllPriceLevels().subscribe({
      next: (levels: any) => {
        const activeLevels = (levels || [])
          .filter((level: PriceLevel) => level.isActive)
          .sort((a: PriceLevel, b: PriceLevel) => a.priority - b.priority);
        this.priceLevels.set(activeLevels);
      },
      error: (error) => {
        console.error('Error loading price levels:', error);
        this.priceLevels.set([]);
      }
    });
  }

  getPriceForLevel(product: Product, priceLevelId: string): ProductPrice | null {
    if (!product?.prices?.length) return null;
    return product.prices.find(price => price.priceLevelId === priceLevelId) || null;
  }

  getProductActivePriceLevelCount(product: Product): number {
    const activeIds = this.activePriceLevelIds();
    const levelIds = product.prices
      .filter(price => price.isActive && activeIds.has(price.priceLevelId))
      .map(price => price.priceLevelId);
    return new Set(levelIds).size;
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.pageNumber.set(page);
      this.loadProducts();
    }
  }

  async deleteProduct(id: string): Promise<void> {
    const confirmed = await this.notificationService.confirm('Estas seguro de eliminar este producto?');
    if (!confirmed) return;

    this.adminService.deleteProduct(id).subscribe({
      next: () => {
        this.loadProducts();
      },
      error: (error) => {
        console.error('Error deleting product:', error);
        this.notificationService.error('Error al eliminar el producto');
      }
    });
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
        this.notificationService.error('Error al actualizar el producto');
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
      this.notificationService.warning('La cantidad debe ser mayor a 0');
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
        this.notificationService.success('Stock agregado correctamente');
      },
      error: (error: any) => {
        console.error('Error creating movement:', error);
        this.notificationService.error('Error al agregar stock: ' + (error.error?.message || error.message));
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

