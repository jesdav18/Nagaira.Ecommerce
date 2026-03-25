import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { NotificationService } from '../../../core/services/notification.service';
import { AppCurrencyPipe } from '../../../core/pipes/currency.pipe';
import { Order } from '../../../core/models/models';

@Component({
  selector: 'app-admin-orders',
  standalone: true,
  imports: [CommonModule, FormsModule, AppCurrencyPipe],
  templateUrl: './admin-orders.component.html',
  styleUrls: ['./admin-orders.component.css']
})
export class AdminOrdersComponent implements OnInit {
  private adminService = inject(AdminService);
  private notificationService = inject(NotificationService);

  orders = signal<Order[]>([]);
  loading = signal(true);
  selectedStatus = signal('Pending');
  updatingOrderId = signal<string | null>(null);
  selectedOrder = signal<Order | null>(null);

  readonly filterOptions = [
    { value: 'Pending', label: 'Pendientes' },
    { value: 'Processing', label: 'En proceso' },
    { value: '', label: 'Recientes' }
  ];

  readonly statusOptions = [
    { value: 'Pending', label: 'Pendiente' },
    { value: 'Processing', label: 'En proceso' },
    { value: 'Shipped', label: 'Enviada' },
    { value: 'Delivered', label: 'Entregada' },
    { value: 'Cancelled', label: 'Cancelada' }
  ];

  ngOnInit(): void {
    this.loadOrders();
  }

  loadOrders(): void {
    this.loading.set(true);
    const status = this.selectedStatus() || undefined;
    this.adminService.getAdminOrders(status).subscribe({
      next: (data: any) => {
        this.orders.set((data ?? []) as Order[]);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading admin orders', error);
        this.notificationService.error('No se pudieron cargar las ordenes.');
        this.loading.set(false);
      }
    });
  }

  onFilterChange(value: string): void {
    this.selectedStatus.set(value);
    this.loadOrders();
  }

  updateStatus(order: Order, status: string): void {
    if (!order?.id || !status || order.status === status) return;

    this.updatingOrderId.set(order.id);
    this.adminService.updateOrderStatus(order.id, status).subscribe({
      next: () => {
        this.orders.update(orders => orders.map(item => item.id === order.id ? { ...item, status } : item));
        this.updatingOrderId.set(null);
      },
      error: (error) => {
        console.error('Error updating order status', error);
        this.notificationService.error(error?.error?.message || 'No se pudo actualizar el estado de la orden.');
        this.updatingOrderId.set(null);
      }
    });
  }

  getCustomerLabel(order: Order): string {
    return order.customerName?.trim() || 'Invitado';
  }

  getContactLine(order: Order): string {
    return order.customerPhone?.trim() || order.customerEmail?.trim() || 'Sin contacto';
  }

  openOrderDetail(order: Order): void {
    this.selectedOrder.set(order);
  }

  closeOrderDetail(): void {
    this.selectedOrder.set(null);
  }

  getStatusClass(status: string): string {
    return (status || '').toLowerCase();
  }
}
