import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HomeHeroComponent } from './home-hero/home-hero.component';
import { HomeFeaturedComponent } from './home-featured/home-featured.component';
import { ProductRequestCtaComponent } from '../product-requests/product-request-cta.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, ProductRequestCtaComponent, HomeHeroComponent, HomeFeaturedComponent],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent {}
