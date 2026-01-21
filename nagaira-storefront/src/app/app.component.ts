import { Component, inject } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { HeaderComponent } from './shared/components/header/header.component';
import { FooterComponent } from './shared/components/footer/footer.component';
import { CommonModule } from '@angular/common';
import { filter } from 'rxjs/operators';
import { AnalyticsService } from './core/services/analytics.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, HeaderComponent, FooterComponent, CommonModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  title = 'Nagaira';
  private router = inject(Router);
  private analyticsService = inject(AnalyticsService);
  showHeader = true;
  showFooter = true;

  constructor() {
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe((event: any) => {
        const path = event.urlAfterRedirects || event.url;
        const isAdmin = path.startsWith('/admin');
        this.showHeader = !isAdmin;
        this.showFooter = !isAdmin;
        if (!isAdmin) {
          this.analyticsService.pageView(path);
        }
      });
  }
}
