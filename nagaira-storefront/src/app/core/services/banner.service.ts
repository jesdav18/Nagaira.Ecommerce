import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Banner } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class BannerService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/banners`;

  getActiveBanners(): Observable<Banner[]> {
    return this.http.get<Banner[]>(this.apiUrl);
  }
}
