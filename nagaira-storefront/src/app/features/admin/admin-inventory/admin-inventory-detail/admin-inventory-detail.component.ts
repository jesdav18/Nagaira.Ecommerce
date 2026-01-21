import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../../core/services/admin.service';
import { NotificationService } from '../../../../core/services/notification.service';

@Component({
  selector: 'app-admin-inventory-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './admin-inventory-detail.component.html',
  styleUrls: ['./admin-inventory-detail.component.css']
})
export class AdminInventoryDetailComponent implements OnInit {
  private adminService = inject(AdminService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private notificationService = inject(NotificationService);
  
  productId = signal<string | null>(null);
  product = signal<any>(null);
  balance = signal<any>(null);
  movements = signal<any[]>([]);
  loading = signal(true);
  showMovementForm = signal(false);
  saving = signal(false);
  
  movementTypes = [
    { value: 'Purchase', label: 'Compra (Entrada)' },
    { value: 'Return', label: 'Devolución (Entrada)' },
    { value: 'Adjustment', label: 'Ajuste (Puede ser + o -)' },
    { value: 'TransferIn', label: 'Transferencia Entrada' },
    { value: 'InitialStock', label: 'Stock Inicial' }
  ];
  
  movementForm = {
    movementType: 'Purchase',
    quantity: 1,
    referenceNumber: '',
    notes: '',
    costPerUnit: null as number | null
  };

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.productId.set(id);
      this.loadData(id);
    }
  }

  loadData(id: string): void {
    this.loading.set(true);
    
    this.adminService.getProductById(id).subscribe({
      next: (product: any) => {
        this.product.set(product);
        this.loadBalance(id);
      },
      error: (error) => {
        console.error('Error loading product:', error);
        this.loading.set(false);
      }
    });
  }

  loadBalance(productId: string): void {
    this.adminService.getProductBalance(productId).subscribe({
      next: (balance: any) => {
        this.balance.set(balance);
        this.loadMovements(productId);
      },
      error: (error) => {
        console.error('Error loading balance:', error);
        this.loadMovements(productId);
      }
    });
  }

  loadMovements(productId: string): void {
    this.adminService.getProductMovements(productId).subscribe({
      next: (movements: any) => {
        this.movements.set(movements);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading movements:', error);
        this.loading.set(false);
      }
    });
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

  toggleMovementForm(): void {
    this.showMovementForm.set(!this.showMovementForm());
    if (!this.showMovementForm()) {
      this.resetForm();
    }
  }

  resetForm(): void {
    this.movementForm = {
      movementType: 'Purchase',
      quantity: 1,
      referenceNumber: '',
      notes: '',
      costPerUnit: null
    };
  }

  createMovement(): void {
    if (!this.productId()) return;
    
    let quantity = this.movementForm.quantity;
    
    if (this.movementForm.movementType === 'Adjustment') {
      quantity = quantity;
    } else {
      quantity = Math.abs(quantity);
    }
    
    const movement = {
      productId: this.productId()!,
      movementType: this.movementForm.movementType,
      quantity: quantity,
      referenceNumber: this.movementForm.referenceNumber || null,
      notes: this.movementForm.notes || null,
      costPerUnit: this.movementForm.costPerUnit || null
    };

    this.saving.set(true);
    this.adminService.createMovement(movement).subscribe({
      next: () => {
        this.saving.set(false);
        this.showMovementForm.set(false);
        this.resetForm();
        this.loadData(this.productId()!);
      },
      error: (error) => {
        console.error('Error creating movement:', error);
        this.notificationService.error('Error al crear el movimiento: ' + (error.error?.message || error.message));
        this.saving.set(false);
      }
    });
  }
}

