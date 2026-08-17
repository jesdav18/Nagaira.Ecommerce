import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';
import { AdminProductFormComponent } from './admin-product-form.component';
import { AdminService } from '../../../../core/services/admin.service';
import { CategoryService } from '../../../../core/services/category.service';
import { AppSettingsService } from '../../../../core/services/app-settings.service';
import { SupplierService } from '../../../../core/services/supplier.service';
import { NotificationService } from '../../../../core/services/notification.service';

describe('AdminProductFormComponent brand selector', () => {
  const brand = { id: 'brand-1', name: 'Nivea', isActive: true, createdAt: '' };
  const api = { getAllCategories: () => of([]), getAllPriceLevels: () => of([]), getBrands: () => of([brand]), createBrand: (name: string) => of({ ...brand, id: 'brand-2', name }) };
  beforeEach(() => TestBed.configureTestingModule({ imports: [AdminProductFormComponent], providers: [
    { provide: AdminService, useValue: api }, { provide: CategoryService, useValue: {} }, { provide: AppSettingsService, useValue: { getTaxRate: () => .16, getCurrencySymbol: () => 'L' } },
    { provide: SupplierService, useValue: { getActiveSuppliers: () => of([]) } }, { provide: NotificationService, useValue: { error: () => {}, warning: () => {} } },
    { provide: ActivatedRoute, useValue: { paramMap: of(convertToParamMap({})) } }, { provide: Router, useValue: { navigate: () => Promise.resolve(true) } }
  ] }));

  it('selects an existing brand from searchable input', () => {
    const component = TestBed.createComponent(AdminProductFormComponent).componentInstance;
    component.brands.set([brand]); component.onBrandSearch('Nivea');
    expect(component.formData.brandId).toBe('brand-1');
  });

  it('creates and automatically selects a new brand without resetting product data', () => {
    const component = TestBed.createComponent(AdminProductFormComponent).componentInstance;
    component.formData.name = 'Producto en edición'; component.newBrandName = 'Nueva Marca'; component.createBrand();
    expect(component.formData.brandId).toBe('brand-2'); expect(component.formData.name).toBe('Producto en edición');
  });
});
