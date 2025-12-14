import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { AppCurrencyPipe } from '../../../core/pipes/currency.pipe';

@Component({
  selector: 'app-admin-kardex',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, AppCurrencyPipe],
  templateUrl: './admin-kardex.component.html',
  styleUrls: ['./admin-kardex.component.css']
})
export class AdminKardexComponent implements OnInit {
  private adminService = inject(AdminService);
  
  movements = signal<any[]>([]);
  loading = signal(true);
  pageNumber = signal(1);
  pageSize = signal(50);
  totalCount = signal(0);
  totalPages = signal(0);
  
  movementTypeFilter = signal('');
  searchTerm = signal('');
  movementTypes = signal<any[]>([]);

  ngOnInit(): void {
    this.loadMovementTypes();
    this.loadMovements();
  }

  loadMovementTypes(): void {
    this.adminService.getMovementTypes().subscribe({
      next: (types: any) => {
        this.movementTypes.set(Array.isArray(types) ? types : []);
      },
      error: (error) => {
        console.error('Error loading movement types:', error);
      }
    });
  }

  loadMovements(): void {
    this.loading.set(true);
    this.adminService.getMovementsPaged(
      this.pageNumber(),
      this.pageSize()
    ).subscribe({
      next: (response: any) => {
        this.movements.set(response.items || []);
        this.totalCount.set(response.totalCount || 0);
        this.totalPages.set(response.totalPages || 0);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading movements:', error);
        this.loading.set(false);
      }
    });
  }

  onSearch(): void {
    this.pageNumber.set(1);
    this.loadMovements();
  }

  onFilterChange(): void {
    this.pageNumber.set(1);
    this.loadMovements();
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.pageNumber.set(page);
      this.loadMovements();
    }
  }

  getMovementTypeLabel(type: string): string {
    const labels: { [key: string]: string } = {
      'Purchase': 'Compra',
      'Sale': 'Venta',
      'Adjustment': 'Ajuste',
      'Return': 'Devolución',
      'TransferIn': 'Transferencia Entrada',
      'TransferOut': 'Transferencia Salida',
      'Damage': 'Daño',
      'Expired': 'Vencido',
      'InitialStock': 'Stock Inicial'
    };
    return labels[type] || type;
  }

  getMovementClass(type: string): string {
    const entryTypes = ['Purchase', 'Return', 'TransferIn', 'InitialStock'];
    const exitTypes = ['Sale', 'TransferOut', 'Damage', 'Expired'];
    
    if (entryTypes.includes(type)) return 'entry';
    if (exitTypes.includes(type)) return 'exit';
    return 'adjustment';
  }

  getFilteredMovements(): any[] {
    let filtered = this.movements();
    
    if (this.movementTypeFilter()) {
      filtered = filtered.filter(m => m.movementType === this.movementTypeFilter());
    }
    
    if (this.searchTerm()) {
      const term = this.searchTerm().toLowerCase();
      filtered = filtered.filter(m => 
        m.productName?.toLowerCase().includes(term) ||
        m.referenceNumber?.toLowerCase().includes(term)
      );
    }
    
    return filtered;
  }
}

