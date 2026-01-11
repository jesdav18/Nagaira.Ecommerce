import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AppSettingsService } from '../../core/services/app-settings.service';

@Component({
  selector: 'app-policy-sales',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './policy-sales.component.html',
  styleUrls: ['./policy-sales.component.css']
})
export class PolicySalesComponent {
  private appSettingsService = inject(AppSettingsService);
  currencySymbol = this.appSettingsService.currencySymbol;
}
