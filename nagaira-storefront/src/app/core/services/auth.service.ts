import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { Router } from '@angular/router';
import { AuthResponse, LoginRequest, RegisterRequest, User } from '../models/models';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private apiUrl = `${environment.apiUrl}/auth`;
  
  currentUser = signal<User | null>(null);
  isAuthenticated = signal<boolean>(false);
  private accessToken = signal<string | null>(null);

  constructor() {
    this.clearLegacyStorage();
    this.bootstrapSession();
  }

  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, credentials, { withCredentials: true })
      .pipe(tap(response => this.handleAuthSuccess(response)));
  }

  register(data: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, data, { withCredentials: true })
      .pipe(tap(response => this.handleAuthSuccess(response)));
  }

  refreshToken(): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/refresh`, {}, { withCredentials: true })
      .pipe(tap(response => this.handleAuthSuccess(response)));
  }

  changePassword(data: { oldPassword: string, newPassword: string }): Observable<any> {
    return this.http.post(`${this.apiUrl}/change-password`, data);
  }

  logout(): void {
    this.http.post(`${this.apiUrl}/logout`, {}, { withCredentials: true }).subscribe({
      next: () => {
        this.clearSession();
      },
      error: () => {
        this.clearSession();
      }
    });
  }

  getToken(): string | null {
    return this.accessToken();
  }

  private handleAuthSuccess(response: AuthResponse): void {
    this.accessToken.set(response.token);
    this.currentUser.set(response.user);
    this.isAuthenticated.set(true);
  }

  private clearSession(): void {
    this.accessToken.set(null);
    this.currentUser.set(null);
    this.isAuthenticated.set(false);
    this.router.navigate(['/']);
  }
  
  private clearLegacyStorage(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
  }

  private bootstrapSession(): void {
    this.refreshToken().subscribe({
      next: () => {},
      error: () => {
        this.clearSession();
      }
    });
  }
}
