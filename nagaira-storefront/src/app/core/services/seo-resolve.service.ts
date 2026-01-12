import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class SeoResolveService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/seo/resolve`;

  resolve(type: 'product' | 'category', slug: string): Observable<{ slug: string }> {
    return this.http.get<{ slug: string }>(this.apiUrl, {
      params: { type, slug }
    });
  }
}
