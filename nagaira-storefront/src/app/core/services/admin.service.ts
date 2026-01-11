import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/admin`;

  getDashboardStats() {
    return this.http.get(`${this.apiUrl}/dashboard/stats`);
  }

  getEnhancedDashboardStats() {
    return this.http.get(`${this.apiUrl}/dashboard/stats/enhanced`);
  }

  getProductsPaged(pageNumber: number = 1, pageSize: number = 20, searchTerm?: string, isActive?: boolean, categoryId?: string) {
    let params: any = { pageNumber, pageSize };
    if (searchTerm) params.searchTerm = searchTerm;
    if (isActive !== undefined) params.isActive = isActive;
    if (categoryId) params.categoryId = categoryId;
    return this.http.get(`${this.apiUrl}/dashboard/products`, { params });
  }

  getOffersPaged(pageNumber: number = 1, pageSize: number = 20, status?: string) {
    let params: any = { pageNumber, pageSize };
    if (status) params.status = status;
    return this.http.get(`${this.apiUrl}/dashboard/offers`, { params });
  }

  getMovementsPaged(pageNumber: number = 1, pageSize: number = 20, productId?: string) {
    let params: any = { pageNumber, pageSize };
    if (productId) params.productId = productId;
    return this.http.get(`${this.apiUrl}/dashboard/movements`, { params });
  }

  getAllProducts() {
    return this.http.get(`${this.apiUrl}/products`);
  }

  getProductById(id: string) {
    return this.http.get(`${this.apiUrl}/products/${id}`);
  }

  createProduct(product: any) {
    return this.http.post(`${this.apiUrl}/products`, product);
  }

  updateProduct(id: string, product: any) {
    return this.http.put(`${this.apiUrl}/products/${id}`, product);
  }

  deleteProduct(id: string) {
    return this.http.delete(`${this.apiUrl}/products/${id}`);
  }

  activateProduct(id: string) {
    return this.http.patch(`${this.apiUrl}/products/${id}/activate`, {});
  }

  deactivateProduct(id: string) {
    return this.http.patch(`${this.apiUrl}/products/${id}/deactivate`, {});
  }

  getAllCategories() {
    return this.http.get(`${this.apiUrl}/categories`);
  }

  getCategoryById(id: string) {
    return this.http.get(`${this.apiUrl}/categories/${id}`);
  }

  createCategory(category: any) {
    return this.http.post(`${this.apiUrl}/categories`, category);
  }

  updateCategory(id: string, category: any) {
    return this.http.put(`${this.apiUrl}/categories/${id}`, category);
  }

  deleteCategory(id: string) {
    return this.http.delete(`${this.apiUrl}/categories/${id}`);
  }

  activateCategory(id: string) {
    return this.http.patch(`${this.apiUrl}/categories/${id}/activate`, {});
  }

  deactivateCategory(id: string) {
    return this.http.patch(`${this.apiUrl}/categories/${id}/deactivate`, {});
  }

  getAllPriceLevels() {
    return this.http.get(`${this.apiUrl}/price-levels`);
  }

  getPriceLevelById(id: string) {
    return this.http.get(`${this.apiUrl}/price-levels/${id}`);
  }

  createPriceLevel(priceLevel: any) {
    return this.http.post(`${this.apiUrl}/price-levels`, priceLevel);
  }

  updatePriceLevel(id: string, priceLevel: any) {
    return this.http.put(`${this.apiUrl}/price-levels/${id}`, priceLevel);
  }

  deletePriceLevel(id: string) {
    return this.http.delete(`${this.apiUrl}/price-levels/${id}`);
  }

  getProductPrices(productId: string) {
    return this.http.get(`${this.apiUrl}/product-prices/product/${productId}`);
  }

  createProductPrice(productId: string, price: any) {
    return this.http.post(`${this.apiUrl}/product-prices`, {
      ...price,
      productId
    });
  }

  updateProductPrice(productId: string, priceId: string, price: any) {
    return this.http.put(`${this.apiUrl}/product-prices/${priceId}`, price);
  }

  deleteProductPrice(productId: string, priceId: string) {
    return this.http.delete(`${this.apiUrl}/product-prices/${priceId}`);
  }

  getProductImages(productId: string) {
    return this.http.get(`${this.apiUrl}/product-images/product/${productId}`);
  }

  createProductImage(image: any) {
    return this.http.post(`${this.apiUrl}/product-images`, image);
  }

  updateProductImage(imageId: string, image: any) {
    return this.http.put(`${this.apiUrl}/product-images/${imageId}`, image);
  }

  deleteProductImage(imageId: string) {
    return this.http.delete(`${this.apiUrl}/product-images/${imageId}`);
  }

  addStockToProduct(productId: string, stockData: any) {
    return this.http.post(`${this.apiUrl}/products/${productId}/stock`, stockData);
  }

  getInventoryReport() {
    return this.http.get(`${this.apiUrl}/inventory/report`);
  }

  getMovementsPagedReport(pageNumber: number = 1, pageSize: number = 50) {
    return this.http.get(`${this.apiUrl}/inventory/movements`, { 
      params: { pageNumber: pageNumber.toString(), pageSize: pageSize.toString() } 
    });
  }

  getMovementTypes() {
    return this.http.get<string[]>(`${this.apiUrl}/inventory/movement-types`);
  }

  createInventoryMovement(movement: any) {
    return this.http.post(`${this.apiUrl}/inventory/movements`, movement);
  }

  createMovement(movement: any) {
    return this.createInventoryMovement(movement);
  }

  getProductMovements(productId: string, pageNumber: number = 1, pageSize: number = 50) {
    return this.http.get(`${this.apiUrl}/inventory/products/${productId}/movements`, {
      params: { pageNumber: pageNumber.toString(), pageSize: pageSize.toString() }
    });
  }

  getProductBalance(productId: string) {
    return this.http.get(`${this.apiUrl}/inventory/products/${productId}`);
  }

  getAllPaymentMethods() {
    return this.http.get(`${this.apiUrl}/payment-methods`);
  }

  getPaymentMethodById(id: string) {
    return this.http.get(`${this.apiUrl}/payment-methods/${id}`);
  }

  createPaymentMethod(paymentMethod: any) {
    return this.http.post(`${this.apiUrl}/payment-methods`, paymentMethod);
  }

  updatePaymentMethod(id: string, paymentMethod: any) {
    return this.http.put(`${this.apiUrl}/payment-methods/${id}`, paymentMethod);
  }

  deletePaymentMethod(id: string) {
    return this.http.delete(`${this.apiUrl}/payment-methods/${id}`);
  }

  activatePaymentMethod(id: string) {
    return this.http.patch(`${this.apiUrl}/payment-methods/${id}/activate`, {});
  }

  deactivatePaymentMethod(id: string) {
    return this.http.patch(`${this.apiUrl}/payment-methods/${id}/deactivate`, {});
  }

  getAllPaymentMethodTypes() {
    return this.http.get(`${this.apiUrl}/payment-method-types`);
  }

  getPaymentMethodTypes() {
    return this.http.get(`${this.apiUrl}/payment-methods/types`);
  }

  getPaymentMethodTypeById(id: string) {
    return this.http.get(`${this.apiUrl}/payment-method-types/${id}`);
  }

  createPaymentMethodType(paymentMethodType: any) {
    return this.http.post(`${this.apiUrl}/payment-method-types`, paymentMethodType);
  }

  updatePaymentMethodType(id: string, paymentMethodType: any) {
    return this.http.put(`${this.apiUrl}/payment-method-types/${id}`, paymentMethodType);
  }

  deletePaymentMethodType(id: string) {
    return this.http.delete(`${this.apiUrl}/payment-method-types/${id}`);
  }

  getAllAppSettings() {
    return this.http.get(`${this.apiUrl}/app-settings`);
  }

  getAppSettings() {
    return this.getAllAppSettings();
  }

  getAppSettingById(id: string) {
    return this.http.get(`${this.apiUrl}/app-settings/${id}`);
  }

  updateAppSetting(id: string, setting: any) {
    return this.http.put(`${this.apiUrl}/app-settings/${id}`, setting);
  }

  getAllOffers() {
    return this.http.get(`${this.apiUrl}/offers`);
  }

  getOfferById(id: string) {
    return this.http.get(`${this.apiUrl}/offers/${id}`);
  }

  createOffer(offer: any) {
    return this.http.post(`${this.apiUrl}/offers`, offer);
  }

  updateOffer(id: string, offer: any) {
    return this.http.put(`${this.apiUrl}/offers/${id}`, offer);
  }

  deleteOffer(id: string) {
    return this.http.delete(`${this.apiUrl}/offers/${id}`);
  }

  activateOffer(id: string) {
    return this.http.patch(`${this.apiUrl}/offers/${id}/activate`, {});
  }

  deactivateOffer(id: string) {
    return this.http.patch(`${this.apiUrl}/offers/${id}/deactivate`, {});
  }

  getAuditLogs(filters: any) {
    let params: any = {};
    if (filters.userId) params.userId = filters.userId;
    if (filters.action) params.action = filters.action;
    if (filters.entityType) params.entityType = filters.entityType;
    if (filters.startDate) params.startDate = filters.startDate;
    if (filters.endDate) params.endDate = filters.endDate;
    if (filters.pageNumber) params.pageNumber = filters.pageNumber;
    if (filters.pageSize) params.pageSize = filters.pageSize;
    return this.http.get(`${this.apiUrl}/audit`, { params });
  }

  exportSalesReport(startDate?: string, endDate?: string): Observable<Blob> {
    let params: any = {};
    if (startDate) params.startDate = startDate;
    if (endDate) params.endDate = endDate;
    return this.http.get(`${this.apiUrl}/reports/sales/export`, { 
      params,
      responseType: 'blob'
    });
  }

  exportInventoryReport(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/reports/inventory/export`, {
      responseType: 'blob'
    });
  }

  uploadImage(file: File, folder?: string): Observable<{ imageUrl: string }> {
    const formData = new FormData();
    formData.append('file', file);
    
    let httpOptions: { params?: HttpParams } = {};
    if (folder) {
      httpOptions.params = new HttpParams().set('folder', folder);
    }
    
    return this.http.post<{ imageUrl: string }>(`${this.apiUrl}/upload/image`, formData, httpOptions);
  }
}
