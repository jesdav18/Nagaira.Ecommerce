import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface PaymentMethod {
  id: string;
  name: string;
  description: string;
  type: string;
  accountNumber: string;
  bankName?: string;
  accountHolderName?: string;
  walletProvider?: string;
  walletNumber?: string;
  qrCodeUrl?: string;
  instructions?: string;
  displayOrder: number;
  isActive: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class PaymentMethodService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  getActivePaymentMethods(): Observable<PaymentMethod[]> {
    return this.http.get<PaymentMethod[]>(`${this.apiUrl}/payment-methods/active`);
  }
}

