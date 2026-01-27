import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AdminService } from '../../../core/services/admin.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-admin-offers',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './admin-offers.component.html',
  styleUrls: ['./admin-offers.component.css']
})
export class AdminOffersComponent implements OnInit {
  private adminService = inject(AdminService);
  private notificationService = inject(NotificationService);
  
  offers = signal<any[]>([]);
  loading = signal(true);
  ruleDrafts = signal<Record<string, { ruleType: string; value: number }>>({});

  ruleTypeOptions = [
    { value: 'min_item_price', label: 'Precio unitario min.' },
    { value: 'max_item_price', label: 'Precio unitario max.' },
    { value: 'min_item_subtotal', label: 'Subtotal item min.' },
    { value: 'max_item_subtotal', label: 'Subtotal item max.' },
    { value: 'min_cart_total', label: 'Total carrito min.' }
  ];

  ngOnInit(): void {
    this.loadOffers();
  }

  loadOffers(): void {
    this.loading.set(true);
    this.adminService.getAllOffers().subscribe({
      next: (offers: any) => {
        this.offers.set(offers);
        this.loading.set(false);
      },
      error: (error: any) => {
        console.error('Error loading offers:', error);
        this.loading.set(false);
      }
    });
  }

  toggleOfferStatus(offer: any): void {
    const action = offer.isActive 
      ? this.adminService.deactivateOffer(offer.id)
      : this.adminService.activateOffer(offer.id);
    
    action.subscribe({
      next: () => {
        this.loadOffers();
      },
      error: (error: any) => {
        console.error('Error updating offer:', error);
        this.notificationService.error('Error al actualizar la oferta');
      }
    });
  }

  getRuleDraft(offerId: string): { ruleType: string; value: number } {
    const drafts = this.ruleDrafts();
    return drafts[offerId] ?? { ruleType: 'min_item_price', value: 0 };
  }

  setRuleDraft(offerId: string, patch: Partial<{ ruleType: string; value: number }>): void {
    const drafts = { ...this.ruleDrafts() };
    const current = drafts[offerId] ?? { ruleType: 'min_item_price', value: 0 };
    drafts[offerId] = { ...current, ...patch };
    this.ruleDrafts.set(drafts);
  }

  getRuleLabel(ruleType: string): string {
    const match = this.ruleTypeOptions.find(option => option.value === ruleType);
    return match ? match.label : ruleType;
  }

  addRule(offer: any): void {
    const draft = this.getRuleDraft(offer.id);
    if (!draft.ruleType || draft.value <= 0) {
      this.notificationService.warning('Ingresa un tipo de regla y un valor mayor a 0');
      return;
    }

    const existing = Array.isArray(offer.rules) ? offer.rules : [];
    const normalizedType = draft.ruleType.trim().toLowerCase();
    const duplicate = existing.some((rule: any) =>
      rule.ruleType?.toLowerCase() === normalizedType && Number(rule.value) === Number(draft.value)
    );
    if (duplicate) {
      this.notificationService.warning('Esta regla ya existe en la oferta');
      return;
    }

    const updatedRules = [...existing, { ruleType: normalizedType, value: Number(draft.value) }];
    this.updateOfferRulesInState(offer.id, updatedRules);
    this.setRuleDraft(offer.id, { value: 0 });
  }

  removeRule(offer: any, index: number): void {
    const existing = Array.isArray(offer.rules) ? offer.rules : [];
    const updatedRules = existing.filter((_: any, idx: number) => idx !== index);
    this.updateOfferRulesInState(offer.id, updatedRules);
  }

  saveRules(offer: any): void {
    const payload = {
      id: offer.id,
      name: offer.name,
      description: offer.description,
      status: offer.status,
      discountPercentage: offer.discountPercentage,
      discountAmount: offer.discountAmount,
      minPurchaseAmount: offer.minPurchaseAmount,
      minQuantity: offer.minQuantity,
      maxUsesPerCustomer: offer.maxUsesPerCustomer,
      totalMaxUses: offer.totalMaxUses,
      startDate: offer.startDate,
      endDate: offer.endDate,
      priority: offer.priority,
      isActive: offer.isActive,
      productIds: offer.productIds ?? [],
      categoryIds: offer.categoryIds ?? [],
      excludedProductIds: offer.excludedProductIds ?? [],
      excludedCategoryIds: offer.excludedCategoryIds ?? [],
      rules: offer.rules ?? []
    };

    this.adminService.updateOffer(offer.id, payload).subscribe({
      next: () => {
        this.notificationService.success('Reglas actualizadas');
        this.loadOffers();
      },
      error: (error: any) => {
        console.error('Error updating offer rules:', error);
        this.notificationService.error('Error al actualizar las reglas');
      }
    });
  }

  private updateOfferRulesInState(offerId: string, rules: any[]): void {
    const updated = this.offers().map(offer =>
      offer.id === offerId ? { ...offer, rules } : offer
    );
    this.offers.set(updated);
  }
}

