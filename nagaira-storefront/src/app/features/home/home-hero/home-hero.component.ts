import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { BannerService } from '../../../core/services/banner.service';
import { Banner } from '../../../core/models/models';

@Component({
  selector: 'app-home-hero',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './home-hero.component.html',
  styleUrls: ['./home-hero.component.css']
})
export class HomeHeroComponent implements OnInit, OnDestroy {
  private bannerService = inject(BannerService);

  banners = signal<Banner[]>([]);
  loading = signal(true);
  currentIndex = signal(0);

  private fallbackBanner: Banner = {
    id: 'fallback',
    title: 'Descubre lo extraordinario',
    subtitle: 'Los mejores productos de tecnologia, moda y mas. Todo en un solo lugar.',
    imageUrl: '',
    linkUrl: null,
    displayOrder: 0,
    isActive: true,
    createdAt: new Date().toISOString()
  };

  private intervalId: number | null = null;

  ngOnInit(): void {
    this.loadBanners();
    this.startAutoPlay();
  }

  ngOnDestroy(): void {
    if (this.intervalId) {
      window.clearInterval(this.intervalId);
      this.intervalId = null;
    }
  }

  loadBanners(): void {
    this.bannerService.getActiveBanners().subscribe({
      next: (banners) => {
        this.banners.set(banners);
        this.currentIndex.set(0);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  slides(): Banner[] {
    const list = this.banners();
    return list.length > 0 ? list : [this.fallbackBanner];
  }

  currentSlide(): Banner {
    const slides = this.slides();
    const index = Math.abs(this.currentIndex()) % slides.length;
    return slides[index];
  }

  nextSlide(): void {
    const slides = this.slides();
    if (slides.length <= 1) return;
    this.currentIndex.set((this.currentIndex() + 1) % slides.length);
  }

  prevSlide(): void {
    const slides = this.slides();
    if (slides.length <= 1) return;
    const nextIndex = this.currentIndex() - 1;
    this.currentIndex.set(nextIndex < 0 ? slides.length - 1 : nextIndex);
  }

  selectSlide(index: number): void {
    const slides = this.slides();
    if (index < 0 || index >= slides.length) return;
    this.currentIndex.set(index);
  }

  private startAutoPlay(): void {
    if (this.intervalId) return;
    this.intervalId = window.setInterval(() => {
      const slides = this.slides();
      if (slides.length <= 1) return;
      this.nextSlide();
    }, 6000);
  }
}
