import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AppSetting {
  id: string;
  key: string;
  value: string | null;
  label: string;
  description: string | null;
  category: string;
  dataType: string;
  displayOrder: number;
  isActive: boolean;
  createdAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class AppSettingsService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;
  
  private settingsLoaded = signal(false);
  private settings: Map<string, string> = new Map();
  
  public currencySymbol = signal<string>('$');
  public currencyPosition = signal<'before' | 'after'>('before');
  public taxRate = signal<number>(0.16);
  public taxLabel = signal<string>('Impuestos');
  public shippingFreeLabel = signal<string>('Gratis');
  public defaultCountry = signal<string>('México');

  constructor() {
    this.loadAllSettings();
  }

  private loadAllSettings(): void {
    this.http.get<AppSetting[]>(`${this.apiUrl}/app-settings/active`).subscribe({
      next: (settings) => {
        this.settings.clear();
        settings.forEach(setting => {
          if (setting.value) {
            this.settings.set(setting.key, setting.value);
          }
        });
        this.updatePublicValues();
        this.settingsLoaded.set(true);
      },
      error: (err) => {
        console.error('Error loading settings:', err);
        this.settingsLoaded.set(true);
      }
    });
  }

  private updatePublicValues(): void {
    this.currencySymbol.set(this.settings.get('currency_symbol') || '$');
    const position = this.settings.get('currency_position') || 'before';
    this.currencyPosition.set(position === 'after' ? 'after' : 'before');
    const taxRateValue = parseFloat(this.settings.get('tax_rate') || '0.16');
    this.taxRate.set(isNaN(taxRateValue) ? 0.16 : taxRateValue);
    this.taxLabel.set(this.settings.get('tax_label') || 'Impuestos');
    this.shippingFreeLabel.set(this.settings.get('shipping_free_label') || 'Gratis');
    this.defaultCountry.set(this.settings.get('default_country') || 'México');
  }

  reloadSettings(): void {
    this.settingsLoaded.set(false);
    this.loadAllSettings();
  }

  isSettingsLoaded(): boolean {
    return this.settingsLoaded();
  }

  getAllSettings(): Observable<AppSetting[]> {
    return this.http.get<AppSetting[]>(`${this.apiUrl}/app-settings/active`);
  }

  getSettingByKey(key: string): Observable<{ key: string; value: string }> {
    return this.http.get<{ key: string; value: string }>(`${this.apiUrl}/app-settings/value/${key}`);
  }

  getCurrencySymbol(): string {
    return this.currencySymbol();
  }

  getTaxRate(): number {
    return this.taxRate();
  }

  getTaxLabel(): string {
    return this.taxLabel();
  }

  getShippingFreeLabel(): string {
    return this.shippingFreeLabel();
  }

  getDefaultCountry(): string {
    return this.defaultCountry();
  }
}
