import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AppSettingsService } from '../../../core/services/app-settings.service';
import { AppCurrencyPipe } from '../../../core/pipes/currency.pipe';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [RouterLink, AppCurrencyPipe],
  templateUrl: './footer.component.html',
  styleUrls: ['./footer.component.css']
})
export class FooterComponent {
  private appSettingsService = inject(AppSettingsService);
  currencySymbol = this.appSettingsService.currencySymbol;
  freeShippingThreshold = 700;
  currentYear = new Date().getFullYear();
}
