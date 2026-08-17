import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { Brand, MetaCatalogAdminProduct, MetaCatalogSummary } from '../../../core/models/models';
import { NotificationService } from '../../../core/services/notification.service';

@Component({ selector: 'app-admin-meta-catalog', standalone: true, imports: [CommonModule, FormsModule], templateUrl: './admin-meta-catalog.component.html', styleUrls: ['./admin-meta-catalog.component.css'] })
export class AdminMetaCatalogComponent implements OnInit {
  private api = inject(AdminService); private notifications = inject(NotificationService);
  summary = signal<MetaCatalogSummary | null>(null); products = signal<MetaCatalogAdminProduct[]>([]); brands = signal<Brand[]>([]);
  selected = signal(new Set<string>()); loading = signal(false); syncing = signal(false); totalCount = signal(0);
  page = 1; pageSize = 20; search = ''; status = ''; brandId = '';
  readonly statuses = [
    ['', 'Todos'], ['NOT_SYNCED', 'No migrados'], ['SYNCED', 'Sincronizados'], ['UPDATE_AVAILABLE', 'Actualización pendiente'],
    ['NOT_ELIGIBLE', 'No elegibles'], ['PROCESSING', 'Procesando'], ['ERROR', 'Errores']
  ];
  ngOnInit(): void { this.api.getBrands('', true).subscribe(x => this.brands.set(x)); this.refresh(); }
  refresh(): void {
    this.loading.set(true);
    this.api.getMetaCatalogSummary().subscribe(x => this.summary.set(x));
    const params = {
      page: this.page,
      pageSize: this.pageSize,
      ...(this.search ? { search: this.search } : {}),
      ...(this.status ? { status: this.status } : {}),
      ...(this.brandId ? { brandId: this.brandId } : {})
    };
    this.api.getMetaCatalogProducts(params).subscribe({
      next: x => { this.products.set(x.items); this.totalCount.set(x.totalCount); this.loading.set(false); }, error: () => this.loading.set(false)
    });
  }
  filter(): void { this.page = 1; this.selected.set(new Set()); this.refresh(); }
  toggle(product: MetaCatalogAdminProduct): void { if (!product.isEligible || product.metaStatus === 'PROCESSING') return; const next = new Set(this.selected()); next.has(product.productId) ? next.delete(product.productId) : next.add(product.productId); this.selected.set(next); }
  togglePage(): void { const selectable = this.products().filter(x => x.isEligible && x.metaStatus !== 'PROCESSING'); const all = selectable.every(x => this.selected().has(x.productId)); const next = new Set(this.selected()); selectable.forEach(x => all ? next.delete(x.productId) : next.add(x.productId)); this.selected.set(next); }
  syncSelected(): void { this.execute([...this.selected()], false); }
  syncOne(product: MetaCatalogAdminProduct): void { this.execute([product.productId], false); }
  async force(product: MetaCatalogAdminProduct): Promise<void> { if (await this.notifications.confirm(`¿Forzar actualización de ${product.name}?`)) this.execute([product.productId], true); }
  showError(product: MetaCatalogAdminProduct): void { this.notifications.error(product.lastErrorMessage || 'No hay detalle de error.'); }
  eligibilityLabel(reason?: string): string {
    const labels: Record<string, string> = {
      missing_brand: 'Falta marca', missing_image: 'Falta imagen', missing_public_price: 'Falta precio público',
      missing_slug: 'Falta enlace/slug', missing_public_base_url: 'Configuración Meta incompleta'
    };
    return reason ? (labels[reason] || reason) : 'No elegible';
  }
  private execute(ids: string[], force: boolean): void {
    if (!ids.length || !this.summary()?.adminSyncEnabled) return;
    this.syncing.set(true);
    this.api.syncMetaCatalogSelected(ids, force).subscribe({ next: result => {
      const s = result.summary; this.notifications.success(`Sincronizados: ${s.synced}; procesando: ${s.processing}; omitidos: ${s.skipped + s.unchanged}; errores: ${s.failed}`);
      this.selected.set(new Set()); this.syncing.set(false); this.refresh();
    }, error: err => { this.syncing.set(false); this.notifications.error(err.error?.message || 'No se pudo sincronizar'); } });
  }
}
