import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppSettingsService } from '../../core/services/app-settings.service';
import { AppCurrencyPipe } from '../../core/pipes/currency.pipe';

@Component({
  selector: 'app-shipping-returns',
  standalone: true,
  imports: [CommonModule, AppCurrencyPipe],
  templateUrl: './shipping-returns.component.html',
  styleUrls: ['./shipping-returns.component.css']
})
export class ShippingReturnsComponent {
  private appSettingsService = inject(AppSettingsService);
  currencySymbol = this.appSettingsService.currencySymbol;
  freeShippingThreshold = 700;
}
