import { Pipe, PipeTransform, inject } from '@angular/core';
import { AppSettingsService } from '../services/app-settings.service';

@Pipe({
  name: 'appCurrency',
  standalone: true
})
export class AppCurrencyPipe implements PipeTransform {
  private appSettings = inject(AppSettingsService);

  transform(value: number | null | undefined, format?: string): string {
    if (value === null || value === undefined || isNaN(value)) {
      return '-';
    }

    const formatStr = format || '1.2-2';
    const decimals = parseInt(formatStr.split('.')[1]?.split('-')[0] || '2');
    const formatted = value.toFixed(decimals);
    
    const symbol = this.appSettings.currencySymbol();
    const position = this.appSettings.currencyPosition();
    
    return position === 'after' ? `${formatted}${symbol}` : `${symbol}${formatted}`;
  }
}
