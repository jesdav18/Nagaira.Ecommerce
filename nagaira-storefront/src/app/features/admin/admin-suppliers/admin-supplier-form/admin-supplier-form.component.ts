import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { SupplierService } from '../../../../core/services/supplier.service';
import { Supplier } from '../../../../core/models/models';

@Component({
  selector: 'app-admin-supplier-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './admin-supplier-form.component.html',
  styleUrls: ['./admin-supplier-form.component.css']
})
export class AdminSupplierFormComponent implements OnInit {
  private supplierService = inject(SupplierService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  supplierId = signal<string | null>(null);
  loading = signal(true);
  saving = signal(false);

  formData = {
    name: '',
    legalName: '',
    taxId: '',
    contactName: '',
    email: '',
    phone: '',
    address: '',
    city: '',
    state: '',
    country: '',
    postalCode: '',
    website: '',
    notes: '',
    paymentTerms: '',
    leadTimeDays: 0,
    minOrderAmount: null as number | null,
    isActive: true
  };

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== 'new') {
      this.supplierId.set(id);
      this.loadSupplier(id);
    } else {
      this.loading.set(false);
    }
  }

  loadSupplier(id: string): void {
    this.supplierService.getSupplierById(id).subscribe({
      next: (supplier) => {
        this.formData = {
          name: supplier.name,
          legalName: supplier.legalName || '',
          taxId: supplier.taxId || '',
          contactName: supplier.contactName || '',
          email: supplier.email || '',
          phone: supplier.phone || '',
          address: supplier.address || '',
          city: supplier.city || '',
          state: supplier.state || '',
          country: supplier.country || '',
          postalCode: supplier.postalCode || '',
          website: supplier.website || '',
          notes: supplier.notes || '',
          paymentTerms: supplier.paymentTerms || '',
          leadTimeDays: supplier.leadTimeDays,
          minOrderAmount: supplier.minOrderAmount || null,
          isActive: supplier.isActive
        };
        this.loading.set(false);
      },
      error: (err: any) => {
        console.error('Error loading supplier:', err);
        alert('Error al cargar el proveedor');
        this.router.navigate(['/admin/suppliers']);
      }
    });
  }

  save(): void {
    if (!this.formData.name.trim()) {
      alert('El nombre es requerido');
      return;
    }

    this.saving.set(true);
    const currentId = this.supplierId();
    const isEdit = !!currentId;

    if (isEdit) {
      this.supplierService.updateSupplier(currentId!, this.formData).subscribe({
        next: () => {
          this.saving.set(false);
          this.router.navigate(['/admin/suppliers']);
        },
        error: (err: any) => {
          console.error('Error saving supplier:', err);
          alert('Error al guardar el proveedor: ' + (err.error?.message || err.message));
          this.saving.set(false);
        }
      });
    } else {
      this.supplierService.createSupplier(this.formData).subscribe({
        next: () => {
          this.saving.set(false);
          this.router.navigate(['/admin/suppliers']);
        },
        error: (err: any) => {
          console.error('Error saving supplier:', err);
          alert('Error al guardar el proveedor: ' + (err.error?.message || err.message));
          this.saving.set(false);
        }
      });
    }
  }

  cancel(): void {
    this.router.navigate(['/admin/suppliers']);
  }
}

