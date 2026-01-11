import { Component, inject } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd, RouterLink } from '@angular/router';
import { HeaderComponent } from './shared/components/header/header.component';
import { CommonModule } from '@angular/common';
import { filter } from 'rxjs/operators';
import { AppSettingsService } from './core/services/app-settings.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, HeaderComponent, CommonModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  title = 'Nagaira';
  private router = inject(Router);
  private appSettingsService = inject(AppSettingsService);
  showHeader = true;
  showFooter = true;
  currencySymbol = this.appSettingsService.currencySymbol;

  constructor() {
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe((event: any) => {
        const isAdmin = event.url.startsWith('/admin');
        this.showHeader = !isAdmin;
        this.showFooter = !isAdmin;
      });
  }
}
