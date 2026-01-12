import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AdminService } from '../../../../core/services/admin.service';

@Component({
  selector: 'app-admin-banner-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './admin-banner-form.component.html',
  styleUrls: ['./admin-banner-form.component.css']
})
export class AdminBannerFormComponent implements OnInit {
  private adminService = inject(AdminService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  bannerId = signal<string | null>(null);
  loading = signal(false);
  saving = signal(false);
  uploadingImage = signal(false);

  formData = {
    title: '',
    subtitle: '',
    imageUrl: '',
    linkUrl: '',
    displayOrder: 0,
    isActive: true
  };

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.bannerId.set(id);
      this.loadBanner(id);
    }
  }

  loadBanner(id: string): void {
    this.loading.set(true);
    this.adminService.getBannerById(id).subscribe({
      next: (banner: any) => {
        this.formData = {
          title: banner.title || '',
          subtitle: banner.subtitle || '',
          imageUrl: banner.imageUrl || '',
          linkUrl: banner.linkUrl || '',
          displayOrder: banner.displayOrder ?? 0,
          isActive: banner.isActive ?? true
        };
        this.loading.set(false);
      },
      error: (error: any) => {
        console.error('Error loading banner:', error);
        this.loading.set(false);
      }
    });
  }

  uploadImage(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.uploadingImage.set(true);
    this.adminService.uploadImage(file, 'banners').subscribe({
      next: (response: any) => {
        this.formData.imageUrl = response.imageUrl;
        this.uploadingImage.set(false);
      },
      error: (error: any) => {
        console.error('Error uploading image:', error);
        this.uploadingImage.set(false);
        alert('Error al subir la imagen');
      }
    });
  }

  removeImage(): void {
    this.formData.imageUrl = '';
  }

  save(): void {
    if (!this.formData.title.trim() || !this.formData.imageUrl.trim()) {
      alert('Titulo e imagen son obligatorios');
      return;
    }

    this.saving.set(true);
    const payload = {
      title: this.formData.title.trim(),
      subtitle: this.formData.subtitle.trim() || null,
      imageUrl: this.formData.imageUrl.trim(),
      linkUrl: this.formData.linkUrl.trim() || null,
      displayOrder: this.formData.displayOrder,
      isActive: this.formData.isActive
    };

    const request = this.bannerId()
      ? this.adminService.updateBanner(this.bannerId()!, payload)
      : this.adminService.createBanner(payload);

    request.subscribe({
      next: () => {
        this.saving.set(false);
        this.router.navigate(['/admin/banners']);
      },
      error: (error: any) => {
        console.error('Error saving banner:', error);
        this.saving.set(false);
        alert('Error al guardar el banner');
      }
    });
  }
}
