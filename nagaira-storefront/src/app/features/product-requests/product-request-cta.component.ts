import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-product-request-cta',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './product-request-cta.component.html',
  styleUrls: ['./product-request-cta.component.css']
})
export class ProductRequestCtaComponent {}
