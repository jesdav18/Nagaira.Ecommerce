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
    // Mapeo simple de estatus numérico o string
    const statusMap: any = {
      1: 'Pendiente',
      2: 'Pagado',
      3: 'Enviado',
      4: 'Entregado',
      5: 'Cancelado'
    };
    return statusMap[status] || 'Desconocido';
  }
}
