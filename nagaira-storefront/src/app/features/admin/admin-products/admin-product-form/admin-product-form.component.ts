import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { firstValueFrom, forkJoin } from 'rxjs';
import { AdminService } from '../../../../core/services/admin.service';
import { CategoryService } from '../../../../core/services/category.service';
import { AppSettingsService } from '../../../../core/services/app-settings.service';
import { Product, ProductPrice, ProductImage, PriceLevel } from '../../../../core/models/models';

@Component({
  selector: 'app-admin-product-form',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './admin-product-form.component.html',
  styleUrls: ['./admin-product-form.component.css']
})
export class AdminProductFormComponent implements OnInit {
  private adminService = inject(AdminService);
  private categoryService = inject(CategoryService);
  private appSettingsService = inject(AppSettingsService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  
  productId = signal<string | null>(null);
  product = signal<Product | null>(null);
  categories = signal<any[]>([]);
  priceLevels = signal<PriceLevel[]>([]);
  productPrices = signal<ProductPrice[]>([]);
  productImages = signal<ProductImage[]>([]);
  loading = signal(true);
  saving = signal(false);
  uploadingImage = signal(false);
  
  formData = {
    name: '',
    description: '',
    sku: '',
    categoryId: '',
    cost: null as number | null,
    isActive: true,
    hasVirtualStock: false
  };

  newPrice = {
    priceLevelId: '',
    price: 0,
    priceWithoutTax: 0,
    minQuantity: 1
  };

  newImage = {
    imageUrl: '',
    altText: '',
    isPrimary: false,
    displayOrder: 0
  };

  ngOnInit(): void {
    this.loadCategories();
    this.loadPriceLevels();
    
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id && id !== 'new') {
        this.productId.set(id);
        this.loadProduct(id);
      } else {
        this.loading.set(false);
      }
    });
  }

  loadCategories(): void {
    this.adminService.getAllCategories().subscribe({
      next: (categories: any) => {
        this.categories.set(categories);
      },
      error: (error) => {
        console.error('Error loading categories:', error);
      }
    });
  }

  loadPriceLevels(): void {
    this.adminService.getAllPriceLevels().subscribe({
      next: (levels: any) => {
        this.priceLevels.set(levels);
      },
      error: (error) => {
        console.error('Error loading price levels:', error);
      }
    });
  }

  loadProduct(id: string): void {
    this.loading.set(true);
    this.adminService.getProductById(id).subscribe({
      next: (product: any) => {
        this.product.set(product);
        this.formData = {
          name: product.name,
          description: product.description,
          sku: product.sku,
          categoryId: product.categoryId,
          cost: product.cost,
          isActive: product.isActive,
          hasVirtualStock: product.hasVirtualStock || false
        };
        this.productPrices.set(product.prices || []);
        this.productImages.set(product.images || []);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading product:', error);
        this.loading.set(false);
      }
    });
  }

  calculateSuggestedPrice(priceLevelId: string): number {
    const cost = this.formData.cost || 0;
    if (cost <= 0) return 0;

    const priceLevel = this.priceLevels().find(l => l.id === priceLevelId);
    if (!priceLevel || !priceLevel.markupPercentage) return 0;

    const markupMultiplier = 1 + (priceLevel.markupPercentage / 100);
    return cost * markupMultiplier;
  }

  getSuggestedPrice(): number {
    if (!this.newPrice.priceLevelId) return 0;
    return this.calculateSuggestedPrice(this.newPrice.priceLevelId);
  }

  getSuggestedPriceFormatted(): string {
    const price = this.getSuggestedPrice();
    return price > 0 ? price.toFixed(2) : '0.00';
  }

  getCurrentMarkupPercentage(): number {
    if (!this.newPrice.priceLevelId) return 0;
    const priceLevel = this.priceLevels().find(l => l.id === this.newPrice.priceLevelId);
    return priceLevel?.markupPercentage || 0;
  }

  getCostFormatted(): string {
    const cost = this.formData.cost || 0;
    return cost > 0 ? cost.toFixed(2) : '0.00';
  }

  onPriceLevelChange(): void {
    if (this.newPrice.priceLevelId) {
      const suggestedPrice = this.calculateSuggestedPrice(this.newPrice.priceLevelId);
      if (suggestedPrice > 0) {
        this.newPrice.price = Math.round(suggestedPrice * 100) / 100;
        this.calculatePriceWithoutTax();
      }
    }
  }

  onCostChange(): void {
    if (this.newPrice.priceLevelId) {
      const suggestedPrice = this.calculateSuggestedPrice(this.newPrice.priceLevelId);
      if (suggestedPrice > 0) {
        this.newPrice.price = Math.round(suggestedPrice * 100) / 100;
        this.calculatePriceWithoutTax();
      }
    }
  }

  calculatePriceWithoutTax(): void {
    if (this.newPrice.price > 0) {
      const taxRate = this.appSettingsService.getTaxRate();
      const taxMultiplier = 1 + taxRate;
      this.newPrice.priceWithoutTax = Math.round((this.newPrice.price / taxMultiplier) * 100) / 100;
    }
  }

  calculatePriceWithTax(): void {
    if (this.newPrice.priceWithoutTax > 0) {
      const taxRate = this.appSettingsService.getTaxRate();
      const taxMultiplier = 1 + taxRate;
      this.newPrice.price = Math.round((this.newPrice.priceWithoutTax * taxMultiplier) * 100) / 100;
    }
  }

  addPrice(): void {
    if (!this.newPrice.priceLevelId || this.newPrice.price <= 0 || this.newPrice.priceWithoutTax <= 0) {
      alert('Por favor complete todos los campos del precio');
      return;
    }

    const priceExists = this.productPrices().some(p => p.priceLevelId === this.newPrice.priceLevelId);
    if (priceExists) {
      alert('Ya existe un precio para este nivel');
      return;
    }

    const tempPrice: ProductPrice = {
      id: `temp-${Date.now()}`,
      productId: this.productId() || '',
      priceLevelId: this.newPrice.priceLevelId,
      priceLevelName: this.priceLevels().find(l => l.id === this.newPrice.priceLevelId)?.name || '',
      price: this.newPrice.price,
      priceWithoutTax: this.newPrice.priceWithoutTax,
      minQuantity: this.newPrice.minQuantity,
      isActive: true
    };

    this.productPrices.set([...this.productPrices(), tempPrice]);
    this.newPrice = { priceLevelId: '', price: 0, priceWithoutTax: 0, minQuantity: 1 };
  }

  removePrice(priceId: string): void {
    this.productPrices.set(this.productPrices().filter(p => p.id !== priceId));
  }

  addImage(): void {
    if (!this.newImage.imageUrl) {
      alert('Por favor ingrese la URL de la imagen');
      return;
    }

    const tempImage: ProductImage = {
      id: `temp-${Date.now()}`,
      imageUrl: this.newImage.imageUrl,
      altText: this.newImage.altText,
      isPrimary: this.newImage.isPrimary,
      displayOrder: this.productImages().length
    };

    if (this.newImage.isPrimary) {
      const currentImages = this.productImages();
      currentImages.forEach(img => img.isPrimary = false);
      this.productImages.set(currentImages);
    }

    this.productImages.set([...this.productImages(), tempImage]);
    this.newImage = { imageUrl: '', altText: '', isPrimary: false, displayOrder: 0 };
  }

  removeImage(imageId: string): void {
    this.productImages.set(this.productImages().filter(img => img.id !== imageId));
  }

  setPrimaryImage(imageId: string): void {
    const images = this.productImages();
    images.forEach(img => {
      img.isPrimary = img.id === imageId;
    });
    this.productImages.set([...images]);
  }

  uploadImage(event: any): void {
    const file = event.target.files[0];
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      alert('Por favor seleccione un archivo de imagen');
      return;
    }

    if (file.size > 10 * 1024 * 1024) {
      alert('La imagen no puede ser mayor a 10MB');
      return;
    }

    this.uploadingImage.set(true);
    
    this.adminService.uploadImage(file, 'products').subscribe({
      next: (response: { imageUrl: string }) => {
        if (response.imageUrl) {
          this.newImage.imageUrl = response.imageUrl;
          this.uploadingImage.set(false);
          alert('Imagen subida correctamente. Puede agregarla al producto.');
        } else {
          alert('Error al subir la imagen: No se recibió la URL');
          this.uploadingImage.set(false);
        }
      },
      error: (error) => {
        console.error('Error al subir imagen:', error);
        alert(error.error?.message || 'Error al subir la imagen');
        this.uploadingImage.set(false);
      }
    });
  }

  save(): void {
    if (!this.formData.name || !this.formData.sku || !this.formData.categoryId) {
      alert('Por favor complete todos los campos requeridos');
      return;
    }

    if (this.productPrices().length === 0) {
      alert('Debe agregar al menos un precio para el producto');
      return;
    }

    this.saving.set(true);
    const currentProductId = this.productId();
    const isEdit = currentProductId !== null && currentProductId !== undefined;
    
    if (isEdit && !currentProductId) {
      alert('Error: No se pudo obtener el ID del producto');
      this.saving.set(false);
      return;
    }

    const newPrices = this.productPrices()
      .filter(p => p.id.startsWith('temp'))
      .map(p => ({
        priceLevelId: p.priceLevelId,
        price: p.price,
        priceWithoutTax: p.priceWithoutTax,
        minQuantity: p.minQuantity
      }));

    const newImages = this.productImages()
      .filter(img => img.id.startsWith('temp'))
      .map(img => ({
        imageUrl: img.imageUrl,
        altText: img.altText,
        isPrimary: img.isPrimary,
        displayOrder: img.displayOrder
      }));

    let productData: any;
    
    if (isEdit) {
      productData = {
        id: currentProductId,
        name: this.formData.name,
        description: this.formData.description,
        cost: this.formData.cost,
        isActive: this.formData.isActive,
        hasVirtualStock: this.formData.hasVirtualStock
      };
    } else {
      productData = {
        name: this.formData.name,
        description: this.formData.description,
        sku: this.formData.sku,
        categoryId: this.formData.categoryId,
        cost: this.formData.cost,
        hasVirtualStock: this.formData.hasVirtualStock,
        prices: newPrices,
        images: newImages
      };
    }

    console.log('Saving product:', { isEdit, productId: currentProductId, productData });

    const operation = isEdit
      ? this.adminService.updateProduct(currentProductId!, productData)
      : this.adminService.createProduct(productData);
    
    operation.subscribe({
      next: (response: any) => {
        console.log('Product saved successfully:', response);
        if (isEdit) {
          this.savePricesAndImages(currentProductId!);
        } else {
          const newProductId = response?.id || response?.Id;
          if (newProductId) {
            this.savePricesAndImages(newProductId);
          } else {
            this.saving.set(false);
            this.router.navigate(['/admin/products']);
          }
        }
      },
      error: (error) => {
        console.error('Error saving product:', error);
        alert('Error al guardar el producto: ' + (error.error?.message || error.message));
        this.saving.set(false);
      }
    });
  }

  private savePricesAndImages(productId: string): void {
    const allPrices = this.productPrices();
    const allImages = this.productImages();
    
    const newPrices = allPrices.filter(p => p.id.startsWith('temp'));
    const newImages = allImages.filter(img => img.id.startsWith('temp'));
    const existingImages = allImages.filter(img => !img.id.startsWith('temp'));

    const operations: Promise<any>[] = [];

    newPrices.forEach(price => {
      operations.push(
        firstValueFrom(
          this.adminService.createProductPrice(productId, {
            productId: productId,
            priceLevelId: price.priceLevelId,
            price: price.price,
            priceWithoutTax: price.priceWithoutTax,
            minQuantity: price.minQuantity
          })
        )
      );
    });

    newImages.forEach(image => {
      operations.push(
        firstValueFrom(
          this.adminService.createProductImage({
            productId: productId,
            imageUrl: image.imageUrl,
            altText: image.altText,
            isPrimary: image.isPrimary,
            displayOrder: image.displayOrder
          })
        )
      );
    });

    existingImages.forEach(image => {
      operations.push(
        firstValueFrom(
          this.adminService.updateProductImage(image.id, {
            imageUrl: image.imageUrl,
            altText: image.altText,
            isPrimary: image.isPrimary,
            displayOrder: image.displayOrder
          })
        )
      );
    });

    if (operations.length === 0) {
      this.saving.set(false);
      this.router.navigate(['/admin/products']);
      return;
    }

    Promise.all(operations)
      .then(() => {
        console.log('Prices and images saved successfully');
        this.saving.set(false);
        this.router.navigate(['/admin/products']);
      })
      .catch(error => {
        console.error('Error saving prices/images:', error);
        alert('Producto guardado pero hubo errores al guardar precios/imágenes: ' + (error.error?.message || error.message));
        this.saving.set(false);
        this.router.navigate(['/admin/products']);
      });
  }
}
