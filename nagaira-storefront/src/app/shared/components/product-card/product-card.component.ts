import { Component, Input, OnChanges, SimpleChanges, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Product } from '../../../core/models/models';
import { CartService } from '../../../core/services/cart.service';
import { AppCurrencyPipe } from '../../../core/pipes/currency.pipe';
import {
  getProductOfferPrice,
  getProductPrice,
  getProductStock,
  getPrimaryImage,
  isVirtualStock,
  shouldShowOffer
} from '../../../core/utils/product.utils';

@Component({
  selector: 'app-product-card',
  standalone: true,
  imports: [CommonModule, RouterLink, AppCurrencyPipe],
  templateUrl: './product-card.component.html',
  styleUrls: ['./product-card.component.css']
})
export class ProductCardComponent implements OnChanges {
  @Input({ required: true }) product!: Product;
  @Input() layout: 'default' | 'wide' = 'default';

  cartService = inject(CartService);
  addingToCart = false;
  selectedImageIndex = signal(0);
  galleryImages: Array<{ imageUrl: string; altText: string }> = [];
  private touchStartX: number | null = null;
  private touchStartY: number | null = null;
  private imageWasSwiped = false;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['product']) {
      this.selectedImageIndex.set(0);
      this.galleryImages = this.buildGalleryImages();
    }
  }

  get basePrice(): number {
    return getProductPrice(this.product);
  }

  get offerPrice(): number | null {
    return getProductOfferPrice(this.product);
  }

  get showOffer(): boolean {
    return shouldShowOffer(this.product);
  }

  get finalPrice(): number {
    return this.showOffer && this.offerPrice !== null ? this.offerPrice : this.basePrice;
  }

  get hasDiscount(): boolean {
    return this.showOffer;
  }

  get discountPercentage(): number {
    if (!this.showOffer || this.offerPrice === null || this.basePrice <= 0) return 0;
    return Math.round(((this.basePrice - this.offerPrice) / this.basePrice) * 100);
  }

  private buildGalleryImages(): Array<{ imageUrl: string; altText: string }> {
    const images = [...(this.product.images ?? [])]
      .filter(image => !!image.imageUrl?.trim())
      .sort((a, b) => Number(b.isPrimary) - Number(a.isPrimary) || a.displayOrder - b.displayOrder);
    const uniqueImages = Array.from(new Map(images.map(image => [image.imageUrl, image])).values());

    return uniqueImages.length > 0
      ? uniqueImages
      : [{ imageUrl: getPrimaryImage(this.product), altText: this.product.name }];
  }

  get currentImage(): { imageUrl: string; altText: string } {
    return this.galleryImages[this.selectedImageIndex()] ?? this.galleryImages[0];
  }

  onImageTouchStart(event: TouchEvent): void {
    if (this.galleryImages.length < 2) return;

    const touch = event.touches[0];
    this.touchStartX = touch.clientX;
    this.touchStartY = touch.clientY;
  }

  onImageTouchEnd(event: TouchEvent): void {
    if (this.galleryImages.length < 2 || this.touchStartX === null || this.touchStartY === null) return;

    const touch = event.changedTouches[0];
    const deltaX = touch.clientX - this.touchStartX;
    const deltaY = touch.clientY - this.touchStartY;
    this.touchStartX = null;
    this.touchStartY = null;

    if (Math.abs(deltaX) < 40 || Math.abs(deltaX) <= Math.abs(deltaY)) return;

    this.imageWasSwiped = true;
    const direction = deltaX < 0 ? 1 : -1;
    const imageCount = this.galleryImages.length;
    this.selectedImageIndex.update(index => (index + direction + imageCount) % imageCount);
    setTimeout(() => this.imageWasSwiped = false, 300);
  }

  onImageClick(event: MouseEvent): void {
    if (!this.imageWasSwiped) return;

    event.preventDefault();
    event.stopPropagation();
  }

  get stock(): number | null {
    return getProductStock(this.product);
  }

  get isVirtual(): boolean {
    return isVirtualStock(this.product);
  }

  addToCart(event: Event): void {
    event.preventDefault();
    event.stopPropagation();

    this.addingToCart = true;
    this.cartService.addToCart(this.product, 1);

    setTimeout(() => {
      this.addingToCart = false;
    }, 1000);
  }
}
