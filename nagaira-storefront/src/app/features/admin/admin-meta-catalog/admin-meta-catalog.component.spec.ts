import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AdminMetaCatalogComponent } from './admin-meta-catalog.component';
import { AdminService } from '../../../core/services/admin.service';
import { NotificationService } from '../../../core/services/notification.service';

describe('AdminMetaCatalogComponent', () => {
  const api = {
    getBrands: () => of([]),
    getMetaCatalogSummary: () => of({ total: 1, synced: 0, notSynced: 0, updateAvailable: 0, processing: 0, errors: 0, notEligible: 1, adminSyncEnabled: true }),
    getMetaCatalogProducts: () => of({ page: 1, pageSize: 20, totalCount: 1, adminSyncEnabled: true, items: [] }),
    syncMetaCatalogSelected: () => of({ summary: { synced: 1, processing: 0, skipped: 0, unchanged: 0, failed: 0 } })
  };
  beforeEach(() => TestBed.configureTestingModule({ imports: [AdminMetaCatalogComponent], providers: [
    { provide: AdminService, useValue: api }, { provide: NotificationService, useValue: { confirm: () => Promise.resolve(true), success: () => {}, error: () => {} } }
  ] }));

  it('creates the Meta Catalog admin screen', () => {
    const fixture = TestBed.createComponent(AdminMetaCatalogComponent); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Meta Catalog');
  });

  it('supports multiple selection for eligible products', () => {
    const component = TestBed.createComponent(AdminMetaCatalogComponent).componentInstance;
    const base: any = { isEligible: true, metaStatus: 'NOT_SYNCED' };
    component.toggle({ ...base, productId: '1' }); component.toggle({ ...base, productId: '2' });
    expect(component.selected().size).toBe(2);
  });

  it('does not select products that are not eligible', () => {
    const component = TestBed.createComponent(AdminMetaCatalogComponent).componentInstance;
    component.toggle({ productId: '1', isEligible: false, metaStatus: 'NOT_ELIGIBLE' } as any);
    expect(component.selected().size).toBe(0);
  });
});
