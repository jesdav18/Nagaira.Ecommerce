import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HomeHeroComponent } from './home-hero/home-hero.component';
import { HomeFeaturedComponent } from './home-featured/home-featured.component';
import { ProductRequestCtaComponent } from '../product-requests/product-request-cta.component';
import { CategoryService } from '../../core/services/category.service';
import { AppSettingsService } from '../../core/services/app-settings.service';
import { Category } from '../../core/models/models';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    ProductRequestCtaComponent,
    HomeHeroComponent,
    HomeFeaturedComponent
  ],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  private categoryService = inject(CategoryService);
  private appSettingsService = inject(AppSettingsService);

  categories = signal<Category[]>([]);
  showAllCategories = signal(false);
  currencySymbol = this.appSettingsService.currencySymbol;

  ngOnInit(): void {
    this.categoryService.getAllActive().subscribe({
      next: (categories) => this.categories.set(categories.filter(category => category.isActive)),
      error: () => this.categories.set([])
    });
  }

  categoryTone(index: number): string {
    return ['tone-blue', 'tone-green', 'tone-teal', 'tone-light', 'tone-deep', 'tone-accent'][index % 6];
  }

  visibleCategories(): Category[] {
    const categories = this.categories();
    return this.showAllCategories() ? categories : categories.slice(0, 4);
  }

  hasMoreCategories(): boolean {
    return this.categories().length > 4;
  }

  toggleCategories(): void {
    this.showAllCategories.update(value => !value);
  }
}
