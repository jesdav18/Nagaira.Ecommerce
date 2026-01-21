import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-admin-payment-methods',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './admin-payment-methods.component.html',
  styleUrls: ['./admin-payment-methods.component.css']
})
export class AdminPaymentMethodsComponent implements OnInit {
  private adminService = inject(AdminService);
  private notificationService = inject(NotificationService);
  
  paymentMethods = signal<any[]>([]);
  loading = signal(true);
  searchTerm = signal('');
  isActiveFilter = signal<boolean | null>(null);
  paymentMethodTypes = signal<any[]>([]);

  ngOnInit(): void {
    this.loadPaymentMethodTypes();
    this.loadPaymentMethods();
  }

  loadPaymentMethodTypes(): void {
    this.adminService.getPaymentMethodTypes().subscribe({
      next: (types: any) => {
        this.paymentMethodTypes.set(Array.isArray(types) ? types : []);
      },
      error: (error: any) => {
        console.error('Error loading payment method types:', error);
      }
    });
  }

  loadPaymentMethods(): void {
    this.loading.set(true);
    this.adminService.getAllPaymentMethods().subscribe({
      next: (paymentMethods: any) => {
        let filtered = Array.isArray(paymentMethods) ? paymentMethods : [];
        
        if (this.isActiveFilter() !== null) {
          filtered = filtered.filter(pm => pm.isActive === this.isActiveFilter());
        }
        
        this.paymentMethods.set(filtered);
        this.loading.set(false);
      },
      error: (error: any) => {
        console.error('Error loading payment methods:', error);
        this.loading.set(false);
      }
    });
  }

  onSearch(): void {
    this.loadPaymentMethods();
  }

  onFilterChange(): void {
    this.loadPaymentMethods();
  }

  filteredPaymentMethods(): any[] {
    const term = this.searchTerm().toLowerCase();
    if (!term) return this.paymentMethods();
    
    return this.paymentMethods().filter(pm => 
      pm.name.toLowerCase().includes(term) ||
      pm.description?.toLowerCase().includes(term) ||
      pm.accountNumber.toLowerCase().includes(term)
    );
  }

  async deletePaymentMethod(id: string): Promise<void> {
    const confirmed = await this.notificationService.confirm('Estas seguro de eliminar este medio de pago?');
    if (!confirmed) return;

    this.adminService.deletePaymentMethod(id).subscribe({
      next: () => {
        this.loadPaymentMethods();
      },
      error: (error) => {
        console.error('Error deleting payment method:', error);
        this.notificationService.error('Error al eliminar el medio de pago');
      }
    });
  }

  togglePaymentMethodStatus(paymentMethod: any): void {
    const action = paymentMethod.isActive 
      ? this.adminService.deactivatePaymentMethod(paymentMethod.id)
      : this.adminService.activatePaymentMethod(paymentMethod.id);
    
    action.subscribe({
      next: () => {
        this.loadPaymentMethods();
      },
      error: (error: any) => {
        console.error('Error updating payment method:', error);
        this.notificationService.error('Error al actualizar el medio de pago');
      }
    });
  }

  getTypeLabel(type: string): string {
    const typeObj = this.paymentMethodTypes().find(t => t.value === type);
    return typeObj ? typeObj.label : type;
  }
}

