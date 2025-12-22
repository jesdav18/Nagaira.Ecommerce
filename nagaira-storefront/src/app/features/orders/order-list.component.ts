import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { OrderService } from '../../core/services/order.service';
import { AppCurrencyPipe } from '../../core/pipes/currency.pipe';
import { Order } from '../../core/models/models';

@Component({
  selector: 'app-order-list',
  standalone: true,
  imports: [CommonModule, RouterLink, AppCurrencyPipe],
  templateUrl: './order-list.component.html',
  styleUrls: ['./order-list.component.css']
})
export class OrderListComponent implements OnInit {
  private orderService = inject(OrderService);

  orders = signal<Order[]>([]);
  loading = signal(true);
  expandedSuppliers = signal<Set<string>>(new Set());

  ngOnInit(): void {
    this.orderService.getMyOrders().subscribe({
      next: (data) => {
        this.orders.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error fetching orders', err);
        this.loading.set(false);
      }
    });
  }

  getStatusLabel(status: any): string {
    const statusMap: any = {
      1: 'Pendiente',
      2: 'Pagado',
      3: 'Enviado',
      4: 'Entregado',
      5: 'Cancelado'
    };
    return statusMap[status] || 'Desconocido';
  }

  toggleSupplierDetails(productId: string): void {
    const current = this.expandedSuppliers();
    const updated = new Set(current);
    if (updated.has(productId)) {
      updated.delete(productId);
    } else {
      updated.add(productId);
    }
    this.expandedSuppliers.set(updated);
  }

  isSupplierExpanded(productId: string): boolean {
    return this.expandedSuppliers().has(productId);
  }

  getSupplierCalculationFormula(item: any): string {
    if (!item.suppliers || item.suppliers.length === 0) return '';
    const parts = item.suppliers.map((s: any) => `${s.quantity} × $${s.unitCost.toFixed(2)}`);
    return parts.join(' + ');
  }

  getSupplierTotalCost(item: any): number {
    if (!item.suppliers || item.suppliers.length === 0) return 0;
    return item.suppliers.reduce((sum: number, s: any) => sum + s.totalCost, 0);
  }
}
