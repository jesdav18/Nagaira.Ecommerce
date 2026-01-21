import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../../core/services/admin.service';
import { NotificationService } from '../../../../core/services/notification.service';

@Component({
  selector: 'app-admin-category-form',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './admin-category-form.component.html',
  styleUrls: ['./admin-category-form.component.css']
})
export class AdminCategoryFormComponent implements OnInit {
  private adminService = inject(AdminService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private notificationService = inject(NotificationService);
  
  categoryId = signal<string | null>(null);
  category = signal<any>(null);
  categories = signal<any[]>([]);
  loading = signal(true);
  saving = signal(false);
  uploadingImage = signal(false);
  
  formData = {
    name: '',
    description: '',
    imageUrl: null as string | null,
    parentCategoryId: null as string | null,
    isActive: true
  };

  ngOnInit(): void {
    this.loadCategories();
    
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id && id !== 'new') {
        this.categoryId.set(id);
        this.loadCategory(id);
      } else {
        this.loading.set(false);
      }
    });
  }

  loadCategories(): void {
    this.adminService.getAllCategories().subscribe({
      next: (categories: any) => {
        const allCategories = Array.isArray(categories) ? categories : [];
        if (this.categoryId()) {
          this.categories.set(allCategories.filter((c: any) => c.id !== this.categoryId()));
        } else {
          this.categories.set(allCategories);
        }
      },
      error: (error) => {
        console.error('Error loading categories:', error);
      }
    });
  }

  loadCategory(id: string): void {
    this.loading.set(true);
    this.adminService.getCategoryById(id).subscribe({
      next: (category: any) => {
        this.category.set(category);
        this.formData = {
          name: category.name,
          description: category.description || '',
          imageUrl: category.imageUrl || null,
          parentCategoryId: category.parentCategoryId || null,
          isActive: category.isActive
        };
        this.loadCategories();
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading category:', error);
        this.loading.set(false);
      }
    });
  }

  uploadImage(event: any): void {
    const file = event.target.files?.[0];
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      this.notificationService.warning('El archivo debe ser una imagen');
      return;
    }

    if (file.size > 10 * 1024 * 1024) {
      this.notificationService.warning('La imagen no puede ser mayor a 10MB');
      return;
    }

    this.uploadingImage.set(true);
    this.adminService.uploadImage(file, 'categories').subscribe({
      next: (response: { imageUrl: string }) => {
        this.formData.imageUrl = response.imageUrl;
        this.uploadingImage.set(false);
      },
      error: (error) => {
        console.error('Error uploading image:', error);
        this.notificationService.error('Error al subir la imagen: ' + (error.error?.message || error.message));
        this.uploadingImage.set(false);
      }
    });
  }

  removeImage(): void {
    this.formData.imageUrl = null;
  }

  save(): void {
    if (!this.formData.name.trim()) {
      this.notificationService.warning('El nombre de la categoria es requerido');
      return;
    }

    this.saving.set(true);
    const currentCategoryId = this.categoryId();
    const isEdit = currentCategoryId !== null && currentCategoryId !== undefined;
    
    if (isEdit && !currentCategoryId) {
      this.notificationService.error('Error: No se pudo obtener el ID de la categoria');
      this.saving.set(false);
      return;
    }
    
    const categoryData = {
      name: this.formData.name.trim(),
      description: this.formData.description.trim(),
      imageUrl: this.formData.imageUrl,
      parentCategoryId: this.formData.parentCategoryId || null,
      isActive: this.formData.isActive
    };
    
    console.log('Saving category:', { isEdit, categoryId: currentCategoryId, categoryData });
    
    const operation = isEdit
      ? this.adminService.updateCategory(currentCategoryId!, categoryData)
      : this.adminService.createCategory(categoryData);
    
    operation.subscribe({
      next: (response) => {
        console.log('Category saved successfully:', response);
        this.router.navigate(['/admin/categories']);
      },
      error: (error) => {
        console.error('Error saving category:', error);
        this.notificationService.error('Error al guardar la categoria: ' + (error.error?.message || error.message));
        this.saving.set(false);
      }
    });
  }
}
