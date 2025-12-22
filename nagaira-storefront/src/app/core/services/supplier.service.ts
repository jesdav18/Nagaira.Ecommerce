import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Supplier, ProductSupplier, SupplierCostHistory } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class SupplierService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/admin`;

  getAllSuppliers(): Observable<Supplier[]> {
    return this.http.get<Supplier[]>(`${this.apiUrl}/suppliers`);
  }

  getActiveSuppliers(): Observable<Supplier[]> {
    return this.http.get<Supplier[]>(`${this.apiUrl}/suppliers/active`);
  }

  getSupplierById(id: string): Observable<Supplier> {
    return this.http.get<Supplier>(`${this.apiUrl}/suppliers/${id}`);
  }

  createSupplier(supplier: any): Observable<Supplier> {
    return this.http.post<Supplier>(`${this.apiUrl}/suppliers`, supplier);
  }

  updateSupplier(id: string, supplier: any): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/suppliers/${id}`, supplier);
  }

  deleteSupplier(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/suppliers/${id}`);
  }

  activateSupplier(id: string): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/suppliers/${id}/activate`, {});
  }

  deactivateSupplier(id: string): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/suppliers/${id}/deactivate`, {});
  }

  getProductSuppliers(productId: string): Observable<ProductSupplier[]> {
    return this.http.get<ProductSupplier[]>(`${this.apiUrl}/product-suppliers/product/${productId}`);
  }

  getSupplierProducts(supplierId: string): Observable<ProductSupplier[]> {
    return this.http.get<ProductSupplier[]>(`${this.apiUrl}/product-suppliers/supplier/${supplierId}`);
  }

  getPrimarySupplier(productId: string): Observable<ProductSupplier> {
    return this.http.get<ProductSupplier>(`${this.apiUrl}/product-suppliers/product/${productId}/primary`);
  }

  createProductSupplier(data: any): Observable<ProductSupplier> {
    return this.http.post<ProductSupplier>(`${this.apiUrl}/product-suppliers`, data);
  }

  updateProductSupplier(id: string, data: any, changeReason?: string): Observable<void> {
    let url = `${this.apiUrl}/product-suppliers/${id}`;
    if (changeReason) {
      url += `?changeReason=${encodeURIComponent(changeReason)}`;
    }
    return this.http.put<void>(url, data);
  }

  getCostHistory(productSupplierId: string): Observable<SupplierCostHistory[]> {
    return this.http.get<SupplierCostHistory[]>(`${this.apiUrl}/product-suppliers/product-supplier/${productSupplierId}/cost-history`);
  }

  deleteProductSupplier(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/product-suppliers/${id}`);
  }

  setAsPrimary(productId: string, supplierId: string): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/product-suppliers/product/${productId}/supplier/${supplierId}/set-primary`, {});
  }

  getBestCost(productId: string): Observable<{ cost: number | null }> {
    return this.http.get<{ cost: number | null }>(`${this.apiUrl}/product-suppliers/product/${productId}/best-cost`);
  }
}

