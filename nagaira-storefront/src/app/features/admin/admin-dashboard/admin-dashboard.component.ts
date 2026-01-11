import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AdminService } from '../../../core/services/admin.service';
import { AppCurrencyPipe } from '../../../core/pipes/currency.pipe';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, AppCurrencyPipe],
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.css']
})
export class AdminDashboardComponent implements OnInit {
  private adminService = inject(AdminService);
  
  stats = signal<any>(null);
  loading = signal(true);

  ngOnInit(): void {
    this.loadDashboardStats();
  }

  loadDashboardStats(): void {
    this.loading.set(true);
    this.adminService.getEnhancedDashboardStats().subscribe({
      next: (data: any) => {
        this.stats.set(data);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading dashboard:', error);
        this.loading.set(false);
      }
    });
  }
}

