import { Injectable, inject } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { DOCUMENT } from '@angular/common';
import { environment } from '../../../environments/environment';

interface SeoConfig {
  title: string;
  description: string;
  image?: string;
  url: string;
  type?: string;
}

@Injectable({
  providedIn: 'root'
})
export class SeoService {
  private titleService = inject(Title);
  private metaService = inject(Meta);
  private document = inject(DOCUMENT);

  setMeta(config: SeoConfig): void {
    const type = config.type || 'website';

    this.titleService.setTitle(config.title);
    this.metaService.updateTag({ name: 'description', content: config.description });
    this.metaService.updateTag({ property: 'og:title', content: config.title });
    this.metaService.updateTag({ property: 'og:description', content: config.description });
    this.metaService.updateTag({ property: 'og:type', content: type });
    this.metaService.updateTag({ property: 'og:url', content: config.url });
    if (config.image) {
      this.metaService.updateTag({ property: 'og:image', content: config.image });
    }
    this.metaService.updateTag({ name: 'twitter:card', content: config.image ? 'summary_large_image' : 'summary' });
    this.metaService.updateTag({ name: 'twitter:title', content: config.title });
    this.metaService.updateTag({ name: 'twitter:description', content: config.description });
    if (config.image) {
      this.metaService.updateTag({ name: 'twitter:image', content: config.image });
    }
  }

  setCanonical(url: string): void {
    const head = this.document.head;
    if (!head) return;
    let link = head.querySelector('link[rel="canonical"]') as HTMLLinkElement | null;
    if (!link) {
      link = this.document.createElement('link');
      link.setAttribute('rel', 'canonical');
      head.appendChild(link);
    }
    link.setAttribute('href', url);
  }

  buildUrl(path: string): string {
    const base = environment.publicBaseUrl || this.getBaseUrl();
    const normalizedBase = base.endsWith('/') ? base.slice(0, -1) : base;
    const normalizedPath = path.startsWith('/') ? path : `/${path}`;
    return `${normalizedBase}${normalizedPath}`;
  }

  private getBaseUrl(): string {
    const baseElement = this.document.querySelector('base');
    const baseHref = baseElement?.getAttribute('href') || '/';
    const origin = this.document.location?.origin || '';
    const normalizedBase = baseHref.startsWith('/') ? baseHref : `/${baseHref}`;
    return `${origin}${normalizedBase}`.replace(/\/$/, '');
  }
}
