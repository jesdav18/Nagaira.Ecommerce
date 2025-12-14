import { Component, OnInit, inject, signal, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule, NgForm } from '@angular/forms';
import { AdminService } from '../../../../core/services/admin.service';

@Component({
  selector: 'app-admin-payment-method-form',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './admin-payment-method-form.component.html',
  styleUrls: ['./admin-payment-method-form.component.css']
})
export class AdminPaymentMethodFormComponent implements OnInit {
  @ViewChild('paymentMethodForm') form?: NgForm;
  
  private adminService = inject(AdminService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  
  paymentMethodId = signal<string | null>(null);
  paymentMethod = signal<any>(null);
  loading = signal(true);
  saving = signal(false);
  paymentMethodTypes = signal<any[]>([]);
  
  formData = {
    name: '',
    description: '',
    type: 'BankAccount',
    accountNumber: '',
    bankName: '',
    accountHolderName: '',
    walletProvider: '',
    walletNumber: '',
    qrCodeUrl: '',
    instructions: '',
    displayOrder: 0,
    isActive: true
  };

  requiresAccountNumber(): boolean {
    return this.formData.type === 'BankAccount';
  }

  requiresWalletInfo(): boolean {
    return this.formData.type === 'ElectronicWallet';
  }

  requiresNoAccountInfo(): boolean {
    return this.formData.type === 'Card' || this.formData.type === 'Cash';
  }

  ngOnInit(): void {
    this.loadPaymentMethodTypes();
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id && id !== 'new') {
        this.paymentMethodId.set(id);
        this.loadPaymentMethod(id);
      } else {
        this.loading.set(false);
      }
    });
  }

  loadPaymentMethodTypes(): void {
    this.adminService.getPaymentMethodTypes().subscribe({
      next: (types: any) => {
        this.paymentMethodTypes.set(Array.isArray(types) ? types : []);
        if (types.length > 0 && !this.paymentMethodId()) {
          this.formData.type = types[0].value;
        }
      },
      error: (error: any) => {
        console.error('Error loading payment method types:', error);
        this.paymentMethodTypes.set([
          { value: 'BankAccount', label: 'Cuenta Bancaria' },
          { value: 'ElectronicWallet', label: 'Billetera Electrónica' }
        ]);
      }
    });
  }

  loadPaymentMethod(id: string): void {
    this.loading.set(true);
    this.adminService.getPaymentMethodById(id).subscribe({
      next: (paymentMethod: any) => {
        this.paymentMethod.set(paymentMethod);
        this.formData = {
          name: paymentMethod.name,
          description: paymentMethod.description || '',
          type: paymentMethod.type,
          accountNumber: paymentMethod.accountNumber || '',
          bankName: paymentMethod.bankName || '',
          accountHolderName: paymentMethod.accountHolderName || '',
          walletProvider: paymentMethod.walletProvider || '',
          walletNumber: paymentMethod.walletNumber || '',
          qrCodeUrl: paymentMethod.qrCodeUrl || '',
          instructions: paymentMethod.instructions || '',
          displayOrder: paymentMethod.displayOrder || 0,
          isActive: paymentMethod.isActive
        };
        this.loading.set(false);
      },
      error: (error: any) => {
        console.error('Error loading payment method:', error);
        this.loading.set(false);
      }
    });
  }

  onTypeChange(): void {
    // Limpiar campos según el tipo seleccionado
    if (this.formData.type === 'BankAccount') {
      this.formData.walletProvider = '';
      this.formData.walletNumber = '';
    } else if (this.formData.type === 'ElectronicWallet') {
      this.formData.bankName = '';
      this.formData.accountHolderName = '';
      this.formData.accountNumber = '';
    } else if (this.formData.type === 'Card' || this.formData.type === 'Cash') {
      this.formData.bankName = '';
      this.formData.accountHolderName = '';
      this.formData.accountNumber = '';
      this.formData.walletProvider = '';
      this.formData.walletNumber = '';
    }
  }

  save(event?: Event): void {
    if (event) {
      event.preventDefault();
    }
    if (!this.formData.name?.trim()) {
      alert('El nombre es requerido');
      return;
    }
    
    if (!this.formData.type) {
      alert('El tipo de medio de pago es requerido');
      return;
    }
    
    if (this.requiresAccountNumber()) {
      const accountNumber = this.formData.accountNumber?.trim();
      if (!accountNumber) {
        alert('El número de cuenta es requerido para cuentas bancarias');
        return;
      }
    }
    
    if (this.requiresWalletInfo()) {
      const walletNumber = this.formData.walletNumber?.trim();
      if (!walletNumber) {
        alert('El número de billetera es requerido para billeteras electrónicas');
        return;
      }
    }

    this.saving.set(true);
    const currentId = this.paymentMethodId();
    const isEdit = currentId !== null && currentId !== undefined;
    
    const paymentMethodData: any = {
      name: this.formData.name.trim(),
      description: this.formData.description.trim() || null,
      type: this.formData.type,
      displayOrder: this.formData.displayOrder,
      isActive: this.formData.isActive
    };

    if (this.formData.type === 'BankAccount') {
      paymentMethodData.accountNumber = this.formData.accountNumber?.trim() || null;
      paymentMethodData.bankName = this.formData.bankName?.trim() || null;
      paymentMethodData.accountHolderName = this.formData.accountHolderName?.trim() || null;
      paymentMethodData.walletProvider = null;
      paymentMethodData.walletNumber = null;
    } else if (this.formData.type === 'ElectronicWallet') {
      paymentMethodData.walletNumber = this.formData.walletNumber?.trim() || null;
      paymentMethodData.walletProvider = this.formData.walletProvider?.trim() || null;
      paymentMethodData.accountNumber = null;
      paymentMethodData.bankName = null;
      paymentMethodData.accountHolderName = null;
    } else {
      paymentMethodData.accountNumber = null;
      paymentMethodData.bankName = null;
      paymentMethodData.accountHolderName = null;
      paymentMethodData.walletProvider = null;
      paymentMethodData.walletNumber = null;
    }

    paymentMethodData.qrCodeUrl = this.formData.qrCodeUrl.trim() || null;
    paymentMethodData.instructions = this.formData.instructions.trim() || null;

    if (isEdit) {
      paymentMethodData.id = currentId;
    }
    
    const operation = isEdit
      ? this.adminService.updatePaymentMethod(currentId!, paymentMethodData)
      : this.adminService.createPaymentMethod(paymentMethodData);
    
    operation.subscribe({
      next: () => {
        this.router.navigate(['/admin/payment-methods']);
      },
      error: (error: any) => {
        console.error('Error saving payment method:', error);
        alert('Error al guardar el medio de pago: ' + (error.error?.message || error.message));
        this.saving.set(false);
      }
    });
  }
}

