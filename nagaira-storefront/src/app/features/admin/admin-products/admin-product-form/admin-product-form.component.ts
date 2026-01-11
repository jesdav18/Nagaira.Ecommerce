import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { firstValueFrom, forkJoin } from 'rxjs';
import { AdminService } from '../../../../core/services/admin.service';
import { CategoryService } from '../../../../core/services/category.service';
import { AppSettingsService } from '../../../../core/services/app-settings.service';
import { SupplierService } from '../../../../core/services/supplier.service';
import { AppCurrencyPipe } from '../../../../core/pipes/currency.pipe';
import { Product, ProductPrice, ProductImage, PriceLevel, Supplier, ProductSupplier, SupplierCostHistory } from '../../../../core/models/models';

@Component({
  selector: 'app-admin-product-form',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, AppCurrencyPipe],
  templateUrl: './admin-product-form.component.html',
  styleUrls: ['./admin-product-form.component.css']
})
export class AdminProductFormComponent implements OnInit {
  private adminService = inject(AdminService);
  private categoryService = inject(CategoryService);
  private appSettingsService = inject(AppSettingsService);
  private supplierService = inject(SupplierService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  
  productId = signal<string | null>(null);
  product = signal<Product | null>(null);
  categories = signal<any[]>([]);
  priceLevels = signal<PriceLevel[]>([]);
  productPrices = signal<ProductPrice[]>([]);
  originalPrices = signal<ProductPrice[]>([]);
  productImages = signal<ProductImage[]>([]);
  originalImages = signal<ProductImage[]>([]);
  suppliers = signal<Supplier[]>([]);
  productSuppliers = signal<ProductSupplier[]>([]);
  loading = signal(true);
  saving = signal(false);
  uploadingImage = signal(false);
  editingCostId = signal<string | null>(null);
  editingCost = signal<number>(0);
  costHistory = signal<any[]>([]);
  showingHistory = signal<string | null>(null);
  
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

  newProductSupplier = {
    supplierId: '',
    supplierSku: '',
    supplierCost: 0,
    isPrimary: false,
    priority: 1,
    leadTimeDays: 0,
    minOrderQuantity: 1,
    notes: '',
    isActive: true
  };

  getNextAvailablePriority(): number {
    const existingPriorities = this.productSuppliers().map(ps => ps.priority).sort((a, b) => a - b);
    if (existingPriorities.length === 0) return 1;
    
    for (let i = 1; i <= existingPriorities.length + 1; i++) {
      if (!existingPriorities.includes(i)) {
        return i;
      }
    }
    return existingPriorities.length + 1;
  }

  hasPrimarySupplier(): boolean {
    return this.productSuppliers().some(ps => ps.isPrimary);
  }

  getPrimarySupplier(): any {
    return this.productSuppliers().find(ps => ps.isPrimary);
  }

  editCost(id: string, currentCost: number): void {
    this.editingCostId.set(id);
    this.editingCost.set(currentCost);
  }

  cancelEditCost(): void {
    this.editingCostId.set(null);
    this.editingCost.set(0);
  }

  saveCost(id: string): void {
    const newCost = this.editingCost();
    if (newCost <= 0) {
      alert('El costo debe ser mayor a 0');
      return;
    }

    const supplier = this.productSuppliers().find(ps => ps.id === id);
    if (!supplier) return;

    const changeReason = prompt('Razón del cambio de costo (opcional):');
    
    this.supplierService.updateProductSupplier(id, {
      id: id,
      supplierSku: supplier.supplierSku,
      supplierCost: newCost,
      isPrimary: supplier.isPrimary,
      priority: supplier.priority,
      leadTimeDays: supplier.leadTimeDays,
      minOrderQuantity: supplier.minOrderQuantity,
      notes: supplier.notes,
      isActive: supplier.isActive
    }, changeReason || undefined).subscribe({
      next: () => {
        if (this.productId()) {
          this.loadProductSuppliers(this.productId()!);
        }
        this.cancelEditCost();
      },
      error: (err: any) => {
        console.error('Error updating cost:', err);
        alert('Error al actualizar el costo: ' + (err.error?.message || err.message));
      }
    });
  }

  viewCostHistory(id: string): void {
    if (this.showingHistory() === id) {
      this.showingHistory.set(null);
      this.costHistory.set([]);
      return;
    }

    this.supplierService.getCostHistory(id).subscribe({
      next: (history) => {
        this.costHistory.set(history);
        this.showingHistory.set(id);
      },
      error: (err: any) => {
        console.error('Error loading cost history:', err);
        alert('Error al cargar el historial: ' + (err.error?.message || err.message));
      }
    });
  }

  ngOnInit(): void {
    this.loadCategories();
    this.loadPriceLevels();
    this.loadSuppliers();
    
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

  loadSuppliers(): void {
    this.supplierService.getActiveSuppliers().subscribe({
      next: (data) => {
        this.suppliers.set(data);
      },
      error: (err: any) => {
        console.error('Error loading suppliers:', err);
      }
    });
  }

  loadProductSuppliers(productId: string): void {
    this.supplierService.getProductSuppliers(productId).subscribe({
      next: (data) => {
        this.productSuppliers.set(data);
      },
      error: (err: any) => {
        console.error('Error loading product suppliers:', err);
      }
    });
  }

  loadProduct(id: string): void {
    this.loading.set(true);
    this.adminService.getProductById(id).subscribe({
      next: (product: any) => {
        this.product.set(product);
        this.loadProductSuppliers(id);
        this.formData = {
          name: product.name,
          description: product.description,
          sku: product.sku,
          categoryId: product.categoryId,
          cost: product.cost,
          isActive: product.isActive,
          hasVirtualStock: product.hasVirtualStock || false
        };
        const prices = product.prices || [];
        this.productPrices.set(prices);
        this.originalPrices.set(prices.map((price: ProductPrice) => ({ ...price })));
        const images = product.images || [];
        this.productImages.set(images);
        this.originalImages.set(images.map((image: ProductImage) => ({ ...image })));
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

  addProductSupplier(): void {
    if (!this.newProductSupplier.supplierId || this.newProductSupplier.supplierCost <= 0) {
      alert('Por favor complete el proveedor y el costo');
      return;
    }

    const productId = this.productId();
    if (!productId) {
      alert('Debe guardar el producto primero');
      return;
    }

    const supplierExists = this.productSuppliers().some(ps => ps.supplierId === this.newProductSupplier.supplierId);
    if (supplierExists) {
      alert('Este proveedor ya está asignado al producto');
      return;
    }

    const priorityExists = this.productSuppliers().some(ps => ps.priority === this.newProductSupplier.priority);
    if (priorityExists) {
      alert(`Ya existe un proveedor con prioridad ${this.newProductSupplier.priority}. Por favor use otra prioridad.`);
      return;
    }

    this.supplierService.createProductSupplier({
      productId: productId,
      supplierId: this.newProductSupplier.supplierId,
      supplierSku: this.newProductSupplier.supplierSku || null,
      supplierCost: this.newProductSupplier.supplierCost,
      isPrimary: this.newProductSupplier.isPrimary,
      priority: this.newProductSupplier.priority,
      leadTimeDays: this.newProductSupplier.leadTimeDays,
      minOrderQuantity: this.newProductSupplier.minOrderQuantity,
      notes: this.newProductSupplier.notes || null,
      isActive: this.newProductSupplier.isActive
    }).subscribe({
      next: () => {
        this.loadProductSuppliers(productId);
        this.newProductSupplier = {
          supplierId: '',
          supplierSku: '',
          supplierCost: 0,
          isPrimary: false,
          priority: this.getNextAvailablePriority(),
          leadTimeDays: 0,
          minOrderQuantity: 1,
          notes: '',
          isActive: true
        };
      },
      error: (err: any) => {
        console.error('Error adding product supplier:', err);
        alert('Error al agregar el proveedor: ' + (err.error?.message || err.message));
      }
    });
  }

  removeProductSupplier(id: string): void {
    if (!confirm('¿Estás seguro de eliminar este proveedor del producto?')) return;

    this.supplierService.deleteProductSupplier(id).subscribe({
      next: () => {
        const productId = this.productId();
        if (productId) {
          this.loadProductSuppliers(productId);
        }
      },
      error: (err: any) => {
        console.error('Error removing product supplier:', err);
        alert('Error al eliminar el proveedor');
      }
    });
  }

  setPrimarySupplier(supplierId: string): void {
    const productId = this.productId();
    if (!productId) return;

    this.supplierService.setAsPrimary(productId, supplierId).subscribe({
      next: () => {
        this.loadProductSuppliers(productId);
        this.loadProduct(productId);
      },
      error: (err: any) => {
        console.error('Error setting primary supplier:', err);
        alert('Error al establecer proveedor primario');
      }
    });
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

    const currentProductId = this.productId();
    const isEdit = currentProductId !== null && currentProductId !== undefined;

    this.saving.set(true);
    
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
    const existingPrices = allPrices.filter(p => !p.id.startsWith('temp'));
    const originalPrices = this.originalPrices();
    const currentPriceIds = new Set(existingPrices.map(price => price.id));
    const deletedPrices = originalPrices.filter(price => !currentPriceIds.has(price.id));
    const newImages = allImages.filter(img => img.id.startsWith('temp'));
    const existingImages = allImages.filter(img => !img.id.startsWith('temp'));
    const originalImages = this.originalImages();
    const currentImageIds = new Set(existingImages.map(image => image.id));
    const deletedImages = originalImages.filter(image => !currentImageIds.has(image.id));

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

    existingPrices.forEach(price => {
      operations.push(
        firstValueFrom(
          this.adminService.updateProductPrice(productId, price.id, {
            id: price.id,
            price: price.price,
            priceWithoutTax: price.priceWithoutTax,
            minQuantity: price.minQuantity,
            isActive: price.isActive
          })
        )
      );
    });

    deletedPrices.forEach(price => {
      operations.push(
        firstValueFrom(
          this.adminService.deleteProductPrice(productId, price.id)
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

    deletedImages.forEach(image => {
      operations.push(
        firstValueFrom(
          this.adminService.deleteProductImage(image.id)
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
