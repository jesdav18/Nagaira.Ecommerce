import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateQuoteRequest, Quote } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class QuoteService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/quotes`;

  createQuote(payload: CreateQuoteRequest): Observable<Quote> {
    return this.http.post<Quote>(this.apiUrl, payload);
  }
}

