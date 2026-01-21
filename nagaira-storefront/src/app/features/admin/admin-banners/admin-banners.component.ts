import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AdminService } from '../../../core/services/admin.service';
import { NotificationService } from '../../../core/services/notification.service';

interface Banner {
  id: string;
  title: string;
  subtitle?: string | null;
  imageUrl: string;
  linkUrl?: string | null;
  displayOrder: number;
  isActive: boolean;
  createdAt: string;
}

@Component({
  selector: 'app-admin-banners',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './admin-banners.component.html',
  styleUrls: ['./admin-banners.component.css']
})
export class AdminBannersComponent implements OnInit {
  private adminService = inject(AdminService);
  private notificationService = inject(NotificationService);

  banners = signal<Banner[]>([]);
  loading = signal(true);

  ngOnInit(): void {
    this.loadBanners();
  }

  loadBanners(): void {
    this.loading.set(true);
    this.adminService.getBanners().subscribe({
      next: (banners: any) => {
        this.banners.set(banners);
        this.loading.set(false);
      },
      error: (error: any) => {
        console.error('Error loading banners:', error);
        this.loading.set(false);
      }
    });
  }

  toggleBannerStatus(banner: Banner): void {
    this.adminService.updateBanner(banner.id, { isActive: !banner.isActive }).subscribe({
      next: () => this.loadBanners(),
      error: (error: any) => {
        console.error('Error updating banner:', error);
        this.notificationService.error('Error al actualizar el banner.');
      }
    });
  }

  async deleteBanner(id: string): Promise<void> {
    const confirmed = await this.notificationService.confirm('Deseas eliminar este banner?');
    if (!confirmed) return;

    this.adminService.deleteBanner(id).subscribe({
      next: () => this.loadBanners(),
      error: (error: any) => {
        console.error('Error deleting banner:', error);
        this.notificationService.error('Error al eliminar el banner.');
      }
    });
  }
}
