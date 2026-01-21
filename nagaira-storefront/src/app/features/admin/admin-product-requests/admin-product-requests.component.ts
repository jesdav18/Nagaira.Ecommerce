import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminService } from '../../../core/services/admin.service';
import { NotificationService } from '../../../core/services/notification.service';

interface ProductRequest {
  id: string;
  name: string;
  phone: string;
  email?: string | null;
  city?: string | null;
  address?: string | null;
  description: string;
  urgency: string;
  link?: string | null;
  imageUrl?: string | null;
  imageName?: string | null;
  status: string;
  createdAt: string;
}

@Component({
  selector: 'app-admin-product-requests',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-product-requests.component.html',
  styleUrls: ['./admin-product-requests.component.css']
})
export class AdminProductRequestsComponent implements OnInit {
  private adminService = inject(AdminService);
  private notificationService = inject(NotificationService);

  requests = signal<ProductRequest[]>([]);
  loading = signal(true);
  expandedId = signal<string | null>(null);
  statusOptions = [
    { value: 'new', label: 'Nuevo' },
    { value: 'in_progress', label: 'En proceso' },
    { value: 'contacted', label: 'Contactado' },
    { value: 'closed', label: 'Cerrado' }
  ];
  urgencyLabels: Record<string, string> = {
    low: 'Baja',
    normal: 'Normal',
    high: 'Alta',
    urgent: 'Urgente'
  };

  ngOnInit(): void {
    this.loadRequests();
  }

  loadRequests(): void {
    this.loading.set(true);
    this.adminService.getProductRequests().subscribe({
      next: (requests: any) => {
        this.requests.set(requests);
        this.loading.set(false);
      },
      error: (error: any) => {
        console.error('Error loading product requests:', error);
        this.loading.set(false);
      }
    });
  }

  toggleDetails(id: string): void {
    this.expandedId.set(this.expandedId() === id ? null : id);
  }

  updateStatus(request: ProductRequest, status: string): void {
    if (request.status === status) {
      return;
    }
    this.adminService.updateProductRequestStatus(request.id, status).subscribe({
      next: () => {
        this.loadRequests();
      },
      error: (error: any) => {
        console.error('Error updating request status:', error);
        this.notificationService.error('Error al actualizar el estado.');
      }
    });
  }

  getUrgencyLabel(value: string): string {
    return this.urgencyLabels[value] || value;
  }
}
