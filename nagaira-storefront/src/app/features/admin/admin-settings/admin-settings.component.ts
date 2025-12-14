import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { AppSettingsService } from '../../../core/services/app-settings.service';

interface AppSetting {
  id: string;
  key: string;
  value: string | null;
  label: string;
  description: string | null;
  category: string;
  dataType: string;
  displayOrder: number;
  isActive: boolean;
  createdAt: string;
}

@Component({
  selector: 'app-admin-settings',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './admin-settings.component.html',
  styleUrls: ['./admin-settings.component.css']
})
export class AdminSettingsComponent implements OnInit {
  private adminService = inject(AdminService);
  private appSettings = inject(AppSettingsService);
  
  settings = signal<AppSetting[]>([]);
  loading = signal(true);
  saving = signal(false);
  searchTerm = signal('');
  categoryFilter = signal('');
  
  categories = signal<string[]>([]);
  groupedSettings = signal<Map<string, AppSetting[]>>(new Map());
  
  getCategoriesArray(): string[] {
    return Array.from(this.groupedSettings().keys());
  }
  
  getSettingsForCategory(category: string): AppSetting[] {
    return this.groupedSettings().get(category) || [];
  }

  ngOnInit(): void {
    this.loadSettings();
  }

  loadSettings(): void {
    this.loading.set(true);
    this.adminService.getAppSettings().subscribe({
      next: (settings: any) => {
        const settingsArray = Array.isArray(settings) ? settings : [];
        this.settings.set(settingsArray);
        
        const categories = new Set<string>();
        settingsArray.forEach((s: AppSetting) => categories.add(s.category));
        this.categories.set(Array.from(categories).sort());
        
        this.groupSettings();
        this.loading.set(false);
      },
      error: (error: any) => {
        console.error('Error loading settings:', error);
        this.loading.set(false);
      }
    });
  }

  groupSettings(): void {
    const grouped = new Map<string, AppSetting[]>();
    const filtered = this.getFilteredSettings();
    
    filtered.forEach(setting => {
      if (!grouped.has(setting.category)) {
        grouped.set(setting.category, []);
      }
      grouped.get(setting.category)!.push(setting);
    });
    
    grouped.forEach((settings, category) => {
      settings.sort((a, b) => a.displayOrder - b.displayOrder);
    });
    
    this.groupedSettings.set(grouped);
  }

  getFilteredSettings(): AppSetting[] {
    let filtered = this.settings();
    
    if (this.searchTerm()) {
      const term = this.searchTerm().toLowerCase();
      filtered = filtered.filter(s => 
        s.key.toLowerCase().includes(term) ||
        s.label.toLowerCase().includes(term) ||
        (s.description && s.description.toLowerCase().includes(term))
      );
    }
    
    if (this.categoryFilter()) {
      filtered = filtered.filter(s => s.category === this.categoryFilter());
    }
    
    return filtered;
  }

  onSearch(): void {
    this.groupSettings();
  }

  onCategoryFilterChange(): void {
    this.groupSettings();
  }

  updateSetting(setting: AppSetting): void {
    this.saving.set(true);
    
    const updateData = {
      id: setting.id,
      key: setting.key,
      value: setting.value,
      label: setting.label,
      description: setting.description,
      category: setting.category,
      dataType: setting.dataType,
      displayOrder: setting.displayOrder,
      isActive: setting.isActive
    };

    this.adminService.updateAppSetting(setting.id, updateData).subscribe({
      next: () => {
        this.appSettings.reloadSettings();
        this.loadSettings();
        this.saving.set(false);
      },
      error: (error) => {
        console.error('Error updating setting:', error);
        alert('Error al actualizar la configuración: ' + (error.error?.message || error.message));
        this.saving.set(false);
      }
    });
  }

  getCategoryLabel(category: string): string {
    const labels: { [key: string]: string } = {
      'currency': 'Moneda',
      'tax': 'Impuestos',
      'shipping': 'Envío',
      'general': 'General'
    };
    return labels[category] || category;
  }

  getDataTypeInputType(dataType: string): string {
    switch (dataType.toLowerCase()) {
      case 'integer':
      case 'decimal':
        return 'number';
      case 'boolean':
        return 'checkbox';
      default:
        return 'text';
    }
  }

  onBooleanChange(setting: AppSetting, event: Event): void {
    const target = event.target as HTMLInputElement;
    setting.value = target.checked ? 'true' : 'false';
    this.updateSetting(setting);
  }
}

