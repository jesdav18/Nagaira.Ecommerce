import { Component, OnInit, OnDestroy, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { AdminService } from '../../../core/services/admin.service';
import { AppCurrencyPipe } from '../../../core/pipes/currency.pipe';
import { PriceLevel, Product, ProductPrice } from '../../../core/models/models';
import { NotificationService } from '../../../core/services/notification.service';
import { AppSettingsService } from '../../../core/services/app-settings.service';

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
  private appSettings = inject(AppSettingsService);
  private searchInput$ = new Subject<string>();
  private destroy$ = new Subject<void>();
  private readonly filterStorageKey = 'admin_products_filters';

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
    this.restoreFilters();
    this.loadMovementTypes();
    this.loadCategories();
    this.loadPriceLevels();
    this.loadProducts();

    this.searchInput$
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe((value) => {
        this.searchTerm.set(value.trim());
        this.pageNumber.set(1);
        this.persistFilters();
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
          { value: 'Return', label: 'Devoluci\u00f3n' },
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
    this.persistFilters();
    this.loadProducts();
  }

  onSearchInput(value: string): void {
    this.searchInput$.next(value ?? '');
  }

  onFilterChange(): void {
    this.pageNumber.set(1);
    this.persistFilters();
    this.loadProducts();
  }

  onTogglePriceLevels(value: boolean): void {
    this.showPriceLevels.set(value);
    this.persistFilters();
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
      this.persistFilters();
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

  downloadCatalog(layout: 'a4' | 'cards'): void {
    this.adminService.getAllProducts().subscribe({
      next: (products: any) => {
        const items = Array.isArray(products) ? products : [];
        if (items.length === 0) {
          this.notificationService.warning('No hay productos para exportar');
          return;
        }
        const html = this.buildCatalogHtml(items, layout);
        this.printHtmlInIframe(html);
      },
      error: (error) => {
        console.error('Error loading products for catalog:', error);
        this.notificationService.error('No se pudo generar el cat\u00e1logo');
      }
    });
  }

  private buildCatalogHtml(products: Product[], layout: 'a4' | 'cards'): string {
    const title = layout === 'a4' ? '' : '';
    const logoUrl = this.toAbsoluteUrl('/assets/images/NagairaLogoNombre.png');
    const cards = products.map(product => this.renderProductCard(product, layout)).join('');

    return `<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8" />
  <title>${this.escapeHtml(title)}</title>
  <style>
    @page { size: A4; margin: 18mm; }
    * { box-sizing: border-box; }
    body { font-family: Arial, sans-serif; color: #1f2937; margin: 0; }
    h1 { font-size: 20px; margin: 0 0 12px 0; }
    .meta { font-size: 12px; color: #6b7280; margin-bottom: 16px; }
    .catalog-header { display: flex; flex-direction: column; align-items: center; gap: 8px; margin-bottom: 16px; }
    .catalog-logo { height: 64px; width: auto; }
    .catalog-meta { text-align: center; }
    .catalog-title { font-size: 18px; font-weight: 700; margin: 0; color: #111827; }
    .grid-a4 { display: grid; grid-template-columns: 1fr; gap: 14px; }
    .grid-cards { display: grid; grid-template-columns: repeat(2, 1fr); gap: 14px; }
    .card { border: 1px solid #e5e7eb; border-radius: 10px; padding: 12px; display: grid; grid-template-columns: 120px 1fr; gap: 12px; }
    .card-img { width: 120px; height: 120px; border-radius: 8px; background: #f3f4f6; overflow: hidden; }
    .card-img img { width: 100%; height: 100%; object-fit: cover; }
    .card-title { font-size: 14px; font-weight: 700; margin: 0 0 6px 0; }
    .card-sku { font-size: 12px; color: #6b7280; margin-bottom: 6px; }
    .card-desc { font-size: 12px; color: #374151; margin-bottom: 8px; line-height: 1.4; }
    .card-meta { font-size: 12px; color: #6b7280; margin-bottom: 6px; }
    .card-price { font-size: 14px; font-weight: 700; color: #8b4513; }
    .page { padding: 18px; }
  </style>
</head>
<body>
  <div class="page">
    <div class="catalog-header">
      <img class="catalog-logo" src="${this.escapeHtml(logoUrl)}" alt="Nagaira" />
      <div class="catalog-meta">
        <div class="catalog-title">Cat&aacute;logo de productos</div>
        <div class="meta">Generado: ${new Date().toLocaleString('es-HN')}</div>
      </div>
    </div>
    <div class="${layout === 'a4' ? 'grid-a4' : 'grid-cards'}">
      ${cards}
    </div>
  </div>
  <script>
    window.onload = () => {
      setTimeout(() => window.print(), 300);
    };
  </script>
</body>
</html>`;
  }

  private printHtmlInIframe(html: string): void {
    const iframe = document.createElement('iframe');
    iframe.style.position = 'fixed';
    iframe.style.right = '0';
    iframe.style.bottom = '0';
    iframe.style.width = '0';
    iframe.style.height = '0';
    iframe.style.border = '0';
    document.body.appendChild(iframe);

    const doc = iframe.contentWindow?.document;
    if (!doc) {
      this.notificationService.error('No se pudo generar el cat\u00e1logo');
      document.body.removeChild(iframe);
      return;
    }

    doc.open();
    doc.write(html);
    doc.close();

    const win = iframe.contentWindow;
    if (!win) {
      this.notificationService.error('No se pudo generar el cat\u00e1logo');
      document.body.removeChild(iframe);
      return;
    }

    const cleanup = () => {
      window.setTimeout(() => {
        if (iframe.parentNode) {
          iframe.parentNode.removeChild(iframe);
        }
      }, 0);
    };

    win.onafterprint = cleanup;

    const images = Array.from(doc.images);
    const waitForImages = (): Promise<void> => {
      if (images.length === 0) return Promise.resolve();
      return new Promise(resolve => {
        let remaining = images.length;
        const done = () => {
          remaining -= 1;
          if (remaining <= 0) resolve();
        };
        images.forEach(img => {
          if (img.complete) {
            done();
          } else {
            img.addEventListener('load', done, { once: true });
            img.addEventListener('error', done, { once: true });
          }
        });
      });
    };

    const timeout = new Promise<void>(resolve => window.setTimeout(resolve, 1500));

    Promise.race([waitForImages(), timeout]).then(() => {
      win.focus();
      win.print();
    });
  }

  private renderProductCard(product: Product, layout: 'a4' | 'cards'): string {
    const imageUrl = this.getPrimaryImage(product);
    const price = this.getMinoristaPrice(product);
    const priceText = price !== null ? this.formatCurrency(price) : 'Sin precio';
    const desc = this.truncate(product.description || '', layout === 'a4' ? 220 : 140);
    return `
      <div class="card">
        <div class="card-img">
          <img src="${this.escapeHtml(imageUrl)}" alt="${this.escapeHtml(product.name)}" />
        </div>
        <div>
          <div class="card-title">${this.escapeHtml(product.name)}</div>
          <div class="card-sku">SKU: ${this.escapeHtml(product.sku)}</div>
          <div class="card-meta">Categor&iacute;a: ${this.escapeHtml(product.categoryName || '-')}</div>
          <div class="card-desc">${this.escapeHtml(desc)}</div>
          <div class="card-price">${this.escapeHtml(priceText)}</div>
        </div>
      </div>
    `;
  }

  private getPrimaryImage(product: Product): string {
    if (!product.images || product.images.length === 0) {
      return this.toAbsoluteUrl('/assets/placeholder.jpg');
    }
    const primary = product.images.find(img => img.isPrimary);
    const url = primary?.imageUrl || product.images[0].imageUrl || '/assets/placeholder.jpg';
    return this.toAbsoluteUrl(url);
  }

  private getMinoristaPrice(product: Product): number | null {
    if (!product.prices || product.prices.length === 0) return null;
    const minorista = product.prices.find(p =>
      p.isActive && (p.priceLevelName || '').toLowerCase().includes('minorista')
    );
    if (minorista) return minorista.price;
    const active = product.prices.filter(p => p.isActive);
    if (active.length === 0) return null;
    return active.sort((a, b) => a.minQuantity - b.minQuantity)[0].price;
  }

  private formatCurrency(value: number): string {
    const symbol = this.appSettings.getCurrencySymbol();
    return `${symbol}${value.toFixed(2)}`;
  }

  private truncate(text: string, max: number): string {
    const clean = text.replace(/\s+/g, ' ').trim();
    if (clean.length <= max) return clean;
    return `${clean.slice(0, max - 1)}...`;
  }

  private escapeHtml(text: string): string {
    return text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  private toAbsoluteUrl(url: string): string {
    if (!url) return url;
    if (url.startsWith('http://') || url.startsWith('https://') || url.startsWith('data:')) {
      return url;
    }
    const origin = window.location.origin;
    if (url.startsWith('/')) {
      return `${origin}${url}`;
    }
    return `${origin}/${url}`;
  }

  private persistFilters(): void {
    const payload = {
      searchTerm: this.searchTerm(),
      isActiveFilter: this.isActiveFilter(),
      isFeaturedFilter: this.isFeaturedFilter(),
      categoryFilter: this.categoryFilter(),
      priceLevelFilter: this.priceLevelFilter(),
      showPriceLevels: this.showPriceLevels(),
      pageNumber: this.pageNumber()
    };
    sessionStorage.setItem(this.filterStorageKey, JSON.stringify(payload));
  }

  private restoreFilters(): void {
    const raw = sessionStorage.getItem(this.filterStorageKey);
    if (!raw) return;
    try {
      const data = JSON.parse(raw) as {
        searchTerm?: string;
        isActiveFilter?: boolean | null;
        isFeaturedFilter?: boolean | null;
        categoryFilter?: string | null;
        priceLevelFilter?: string | null;
        showPriceLevels?: boolean;
        pageNumber?: number;
      };
      this.searchTerm.set(typeof data.searchTerm === 'string' ? data.searchTerm : '');
      this.isActiveFilter.set(typeof data.isActiveFilter === 'boolean' ? data.isActiveFilter : null);
      this.isFeaturedFilter.set(typeof data.isFeaturedFilter === 'boolean' ? data.isFeaturedFilter : null);
      this.categoryFilter.set(typeof data.categoryFilter === 'string' ? data.categoryFilter : null);
      this.priceLevelFilter.set(typeof data.priceLevelFilter === 'string' ? data.priceLevelFilter : null);
      this.showPriceLevels.set(typeof data.showPriceLevels === 'boolean' ? data.showPriceLevels : false);
      this.pageNumber.set(typeof data.pageNumber === 'number' && data.pageNumber > 0 ? data.pageNumber : 1);
    } catch {
      sessionStorage.removeItem(this.filterStorageKey);
    }
  }
}
