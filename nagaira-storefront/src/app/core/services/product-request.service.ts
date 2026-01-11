import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ProductRequest {
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

@Injectable({
  providedIn: 'root'
})
export class ProductRequestService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/product-requests`;

  createRequest(payload: {
    description: string;
    name: string;
    phone: string;
    urgency: string;
    email?: string;
    city?: string;
    address?: string;
    link?: string;
    image?: File | null;
  }): Observable<ProductRequest> {
    const formData = new FormData();
    formData.append('description', payload.description);
    formData.append('name', payload.name);
    formData.append('phone', payload.phone);
    formData.append('urgency', payload.urgency);
    if (payload.email && payload.email.trim().length > 0) {
      formData.append('email', payload.email.trim());
    }
    if (payload.city && payload.city.trim().length > 0) {
      formData.append('city', payload.city.trim());
    }
    if (payload.address && payload.address.trim().length > 0) {
      formData.append('address', payload.address.trim());
    }
    if (payload.link && payload.link.trim().length > 0) {
      formData.append('link', payload.link.trim());
    }
    if (payload.image) {
      formData.append('image', payload.image);
    }
    return this.http.post<ProductRequest>(this.apiUrl, formData);
  }
}
