import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AdminService } from '../../../../core/services/admin.service';
import { NotificationService } from '../../../../core/services/notification.service';

type OfferRule = { ruleType: string; value: number };

@Component({
  selector: 'app-admin-offer-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './admin-offer-form.component.html',
  styleUrls: ['./admin-offer-form.component.css']
})
export class AdminOfferFormComponent implements OnInit {
  private adminService = inject(AdminService);
  private notificationService = inject(NotificationService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  offerId = signal<string | null>(null);
  loading = signal(true);
  saving = signal(false);
  categories = signal<any[]>([]);
  products = signal<any[]>([]);
  applyCategoryQuery = signal('');
  applyProductQuery = signal('');
  excludeCategoryQuery = signal('');
  excludeProductQuery = signal('');

  formData = {
    name: '',
    description: '',
    offerType: 'Percentage',
    status: 'Draft',
    discountPercentage: null as number | null,
    discountAmount: null as number | null,
    minPurchaseAmount: null as number | null,
    minQuantity: null as number | null,
    maxUsesPerCustomer: null as number | null,
    totalMaxUses: null as number | null,
    startDate: '',
    endDate: '',
    priority: 0,
    isActive: true,
    productIds: [] as string[],
    categoryIds: [] as string[],
    excludedProductIds: [] as string[],
    excludedCategoryIds: [] as string[],
    rules: [] as OfferRule[]
  };

  ruleDraft = signal<OfferRule>({ ruleType: 'min_item_price', value: 0 });

  ruleTypeOptions = [
    { value: 'min_item_price', label: 'Precio unitario min.' },
    { value: 'max_item_price', label: 'Precio unitario max.' },
    { value: 'min_item_subtotal', label: 'Subtotal item min.' },
    { value: 'max_item_subtotal', label: 'Subtotal item max.' },
    { value: 'min_cart_total', label: 'Total carrito min.' }
  ];

  ngOnInit(): void {
    this.loadCategories();
    this.loadProducts();

    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id && id !== 'new') {
        this.offerId.set(id);
        this.loadOffer(id);
      } else {
        this.loading.set(false);
      }
    });
  }

  private loadCategories(): void {
    this.adminService.getAllCategories().subscribe({
      next: (categories: any) => {
        this.categories.set(Array.isArray(categories) ? categories : []);
      },
      error: (error: any) => {
        console.error('Error loading categories:', error);
        this.categories.set([]);
      }
    });
  }

  private loadProducts(): void {
    this.adminService.getAllProducts().subscribe({
      next: (products: any) => {
        this.products.set(Array.isArray(products) ? products : []);
      },
      error: (error: any) => {
        console.error('Error loading products:', error);
        this.products.set([]);
      }
    });
  }

  filteredCategories(list: any[], query: string, selected: string[]): any[] {
    const q = query.trim().toLowerCase();
    return list
      .filter(item => !selected.includes(item.id))
      .filter(item => !q || (item.name ?? '').toLowerCase().includes(q));
  }

  filteredProducts(list: any[], query: string, selected: string[]): any[] {
    const q = query.trim().toLowerCase();
    return list
      .filter(item => !selected.includes(item.id))
      .filter(item => !q || (item.name ?? '').toLowerCase().includes(q));
  }

  getCategoryName(id: string): string {
    return this.categories().find(c => c.id === id)?.name ?? id;
  }

  getProductName(id: string): string {
    return this.products().find(p => p.id === id)?.name ?? id;
  }

  toggleId(list: string[], id: string): string[] {
    return list.includes(id) ? list.filter(item => item !== id) : [...list, id];
  }

  removeId(list: string[], id: string): string[] {
    return list.filter(item => item !== id);
  }

  selectAllCategories(current: string[], source: any[]): string[] {
    const allIds = source.map(item => item.id);
    return Array.from(new Set([...current, ...allIds]));
  }

  selectAllProducts(current: string[], source: any[]): string[] {
    const allIds = source.map(item => item.id);
    return Array.from(new Set([...current, ...allIds]));
  }

  clearAll(): string[] {
    return [];
  }

  private loadOffer(id: string): void {
    this.loading.set(true);
    this.adminService.getOfferById(id).subscribe({
      next: (offer: any) => {
        this.formData = {
          name: offer.name ?? '',
          description: offer.description ?? '',
          offerType: offer.offerType ?? 'Percentage',
          status: offer.status ?? 'Draft',
          discountPercentage: offer.discountPercentage ?? null,
          discountAmount: offer.discountAmount ?? null,
          minPurchaseAmount: offer.minPurchaseAmount ?? null,
          minQuantity: offer.minQuantity ?? null,
          maxUsesPerCustomer: offer.maxUsesPerCustomer ?? null,
          totalMaxUses: offer.totalMaxUses ?? null,
          startDate: this.toLocalDatetimeInput(offer.startDate),
          endDate: this.toLocalDatetimeInput(offer.endDate),
          priority: offer.priority ?? 0,
          isActive: offer.isActive ?? true,
          productIds: offer.productIds ?? [],
          categoryIds: offer.categoryIds ?? [],
          excludedProductIds: offer.excludedProductIds ?? [],
          excludedCategoryIds: offer.excludedCategoryIds ?? [],
          rules: Array.isArray(offer.rules) ? offer.rules : []
        };
        this.loading.set(false);
      },
      error: (error: any) => {
        console.error('Error loading offer:', error);
        this.loading.set(false);
      }
    });
  }

  addRule(): void {
    const draft = this.ruleDraft();
    if (!draft.ruleType || draft.value <= 0) {
      this.notificationService.warning('Ingresa un tipo de regla y un valor mayor a 0');
      return;
    }

    const normalizedType = draft.ruleType.trim().toLowerCase();
    const duplicate = this.formData.rules.some(rule =>
      rule.ruleType?.toLowerCase() === normalizedType && Number(rule.value) === Number(draft.value)
    );
    if (duplicate) {
      this.notificationService.warning('Esta regla ya existe');
      return;
    }

    this.formData.rules = [...this.formData.rules, { ruleType: normalizedType, value: Number(draft.value) }];
    this.ruleDraft.set({ ...draft, value: 0 });
  }

  setRuleDraft(patch: Partial<OfferRule>): void {
    this.ruleDraft.set({ ...this.ruleDraft(), ...patch });
  }

  removeRule(index: number): void {
    this.formData.rules = this.formData.rules.filter((_, i) => i !== index);
  }

  save(): void {
    if (!this.formData.name) {
      this.notificationService.warning('El nombre es requerido');
      return;
    }

    if (!this.formData.startDate || !this.formData.endDate) {
      this.notificationService.warning('Las fechas de inicio y fin son requeridas');
      return;
    }

    const offerId = this.offerId();
    const payload = {
      ...this.formData,
      id: offerId ?? undefined,
      rules: this.formData.rules ?? []
    };

    this.saving.set(true);
    const request = offerId
      ? this.adminService.updateOffer(offerId, payload)
      : this.adminService.createOffer(payload);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.notificationService.success('Oferta guardada');
        this.router.navigate(['/admin/offers']);
      },
      error: (error: any) => {
        console.error('Error saving offer:', error);
        this.notificationService.error('Error al guardar la oferta');
        this.saving.set(false);
      }
    });
  }

  private toLocalDatetimeInput(value: string | null | undefined): string {
    if (!value) return '';
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return '';
    const pad = (num: number) => `${num}`.padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }
}
