import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SupplierService } from '../../../core/services/supplier.service';
import { Supplier } from '../../../core/models/models';

@Component({
  selector: 'app-admin-suppliers',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './admin-suppliers.component.html',
  styleUrls: ['./admin-suppliers.component.css']
})
export class AdminSuppliersComponent implements OnInit {
  private supplierService = inject(SupplierService);
  
  suppliers = signal<Supplier[]>([]);
  loading = signal(true);
  searchTerm = signal('');

  ngOnInit(): void {
    this.loadSuppliers();
  }

  loadSuppliers(): void {
    this.loading.set(true);
    this.supplierService.getAllSuppliers().subscribe({
      next: (data) => {
        this.suppliers.set(data);
        this.loading.set(false);
      },
      error: (err: any) => {
        console.error('Error loading suppliers:', err);
        this.loading.set(false);
      }
    });
  }

  toggleStatus(supplier: Supplier): void {
    const action = supplier.isActive 
      ? this.supplierService.deactivateSupplier(supplier.id)
      : this.supplierService.activateSupplier(supplier.id);

    action.subscribe({
      next: () => {
        this.loadSuppliers();
      },
      error: (err: any) => {
        console.error('Error toggling supplier status:', err);
        alert('Error al cambiar el estado del proveedor');
      }
    });
  }

  deleteSupplier(id: string): void {
    if (!confirm('¿Estás seguro de eliminar este proveedor?')) return;

    this.supplierService.deleteSupplier(id).subscribe({
      next: () => {
        this.loadSuppliers();
      },
      error: (err: any) => {
        console.error('Error deleting supplier:', err);
        alert('Error al eliminar el proveedor');
      }
    });
  }

  get filteredSuppliers(): Supplier[] {
    const term = this.searchTerm().toLowerCase();
    if (!term) return this.suppliers();
    
    return this.suppliers().filter(s => 
      s.name.toLowerCase().includes(term) ||
      s.email?.toLowerCase().includes(term) ||
      s.taxId?.toLowerCase().includes(term)
    );
  }
}

