import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { from, Observable, switchMap } from 'rxjs';
import { Product } from '../models/models';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private apiUrl = `${environment.apiUrl}/products`;

  getAll(): Observable<Product[]> {
    return this.afterSessionReady(() => this.http.get<Product[]>(this.apiUrl, { withCredentials: true }));
  }

  getById(id: string): Observable<Product> {
    return this.afterSessionReady(() => this.http.get<Product>(`${this.apiUrl}/${id}`, { withCredentials: true }));
  }

  getBySlug(slug: string): Observable<Product> {
    return this.afterSessionReady(() => this.http.get<Product>(`${this.apiUrl}/slug/${slug}`, { withCredentials: true }));
  }

  getByCategory(categoryId: string): Observable<Product[]> {
    return this.afterSessionReady(() => this.http.get<Product[]>(`${this.apiUrl}/category/${categoryId}`, { withCredentials: true }));
  }

  getFeatured(): Observable<Product[]> {
    return this.afterSessionReady(() => this.http.get<Product[]>(`${this.apiUrl}/featured`, { withCredentials: true }));
  }

  search(term: string): Observable<Product[]> {
    return this.afterSessionReady(() => this.http.get<Product[]>(`${this.apiUrl}/search`, {
      withCredentials: true,
      params: { term }
    }));
  }

  private afterSessionReady<T>(request: () => Observable<T>): Observable<T> {
    return from(this.authService.waitForSessionReady()).pipe(
      switchMap(() => request())
    );
  }
}
