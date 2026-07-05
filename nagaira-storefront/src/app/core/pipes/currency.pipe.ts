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
    const decimalsConfig = formatStr.split('.')[1] || '2-2';
    const [minimumFractionDigits, maximumFractionDigits] = decimalsConfig
      .split('-')
      .map(part => parseInt(part, 10));
    const formatted = new Intl.NumberFormat('en-US', {
      minimumFractionDigits: Number.isNaN(minimumFractionDigits) ? 2 : minimumFractionDigits,
      maximumFractionDigits: Number.isNaN(maximumFractionDigits) ? 2 : maximumFractionDigits
    }).format(value);

    const symbol = this.appSettings.currencySymbol();
    const position = this.appSettings.currencyPosition();

    return position === 'after' ? `${formatted} ${symbol}` : `${symbol} ${formatted}`;
  }
}
