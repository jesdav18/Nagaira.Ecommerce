import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

type AnalyticsEventName =
  | 'page_view'
  | 'view_product'
  | 'add_to_cart'
  | 'begin_checkout'
  | 'purchase';

interface AnalyticsEventPayload {
  eventName: AnalyticsEventName;
  anonUserId: string;
  sessionId: string;
  path?: string;
  referrer?: string;
  utmSource?: string;
  utmMedium?: string;
  utmCampaign?: string;
  utmTerm?: string;
  utmContent?: string;
  orderId?: string;
  value?: number;
  currency?: string;
  meta?: Record<string, any>;
}

interface UtmData {
  utm_source?: string;
  utm_medium?: string;
  utm_campaign?: string;
  utm_term?: string;
  utm_content?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AnalyticsService {
  private readonly apiUrl = `${environment.apiUrl}/analytics/event`;
  private readonly anonUserId = this.getOrCreateAnonId();
  private sessionId = this.getOrCreateSessionId();
  private lastPath: string | null = null;
  private readonly utm: UtmData | null = this.getOrCaptureUtm();

  pageView(path: string): void {
    if (!path) return;
    const payload = this.createBasePayload('page_view', path);
    this.send(payload);
    this.lastPath = path;
  }

  viewProduct(product: { id: string; name?: string; price?: number }): void {
    if (!product?.id) return;
    const payload = this.createBasePayload('view_product', this.lastPath || window.location.pathname);
    payload.meta = { productId: product.id, name: product.name, price: product.price };
    this.send(payload);
  }

  addToCart(product: { id: string; name?: string; price?: number }, quantity: number): void {
    if (!product?.id || quantity <= 0) return;
    const payload = this.createBasePayload('add_to_cart', this.lastPath || window.location.pathname);
    payload.meta = { productId: product.id, name: product.name, price: product.price, quantity };
    payload.value = product.price ? product.price * quantity : undefined;
    this.send(payload);
  }

  beginCheckout(cartTotal: number, itemsCount: number): void {
    const payload = this.createBasePayload('begin_checkout', this.lastPath || window.location.pathname);
    payload.value = cartTotal;
    payload.meta = { itemsCount };
    this.send(payload);
  }

  purchase(orderId: string, total: number, currency?: string, meta?: Record<string, any>): void {
    if (!orderId) return;
    const payload = this.createBasePayload('purchase', this.lastPath || window.location.pathname);
    payload.orderId = orderId;
    payload.value = total;
    payload.currency = currency;
    payload.meta = meta;
    this.send(payload);
  }

  private createBasePayload(eventName: AnalyticsEventName, path: string): AnalyticsEventPayload {
    this.sessionId = this.getOrCreateSessionId();
    return {
      eventName,
      anonUserId: this.anonUserId,
      sessionId: this.sessionId,
      path,
      referrer: this.lastPath || document.referrer || undefined,
      utmSource: this.utm?.utm_source,
      utmMedium: this.utm?.utm_medium,
      utmCampaign: this.utm?.utm_campaign,
      utmTerm: this.utm?.utm_term,
      utmContent: this.utm?.utm_content
    };
  }

  private send(payload: AnalyticsEventPayload): void {
    try {
      if (navigator.sendBeacon) {
        const blob = new Blob([JSON.stringify(payload)], { type: 'application/json' });
        navigator.sendBeacon(this.apiUrl, blob);
        return;
      }
    } catch {
    }

    fetch(this.apiUrl, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
      keepalive: true
    }).catch(() => undefined);
  }

  private getOrCreateAnonId(): string {
    const storageKey = 'analytics_anon_id';
    const existing = this.safeStorageGet(localStorage, storageKey);
    if (existing) return existing;
    const value = this.generateId();
    this.safeStorageSet(localStorage, storageKey, value);
    return value;
  }

  private getOrCreateSessionId(): string {
    const storageKey = 'analytics_session';
    const now = Date.now();
    const raw = this.safeStorageGet(sessionStorage, storageKey);
    if (raw) {
      try {
        const parsed = JSON.parse(raw) as { id: string; ts: number };
        if (parsed?.id && parsed?.ts && now - parsed.ts < 30 * 60 * 1000) {
          return parsed.id;
        }
      } catch {
      }
    }
    const id = this.generateId();
    this.safeStorageSet(sessionStorage, storageKey, JSON.stringify({ id, ts: now }));
    return id;
  }

  private getOrCaptureUtm(): UtmData | null {
    const storageKey = 'analytics_utm';
    const existing = this.safeStorageGet(localStorage, storageKey);
    if (existing) {
      try {
        return JSON.parse(existing) as UtmData;
      } catch {
      }
    }

    const params = new URLSearchParams(window.location.search);
    const utm: UtmData = {};
    ['utm_source', 'utm_medium', 'utm_campaign', 'utm_term', 'utm_content'].forEach(key => {
      const value = params.get(key);
      if (value) utm[key as keyof UtmData] = value;
    });
    if (Object.keys(utm).length === 0) return null;
    this.safeStorageSet(localStorage, storageKey, JSON.stringify(utm));
    return utm;
  }

  private safeStorageGet(storage: Storage, key: string): string | null {
    try {
      return storage.getItem(key);
    } catch {
      return null;
    }
  }

  private safeStorageSet(storage: Storage, key: string, value: string): void {
    try {
      storage.setItem(key, value);
    } catch {
    }
  }

  private generateId(): string {
    if (crypto?.randomUUID) {
      return crypto.randomUUID();
    }
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, c => {
      const r = Math.random() * 16 | 0;
      const v = c === 'x' ? r : (r & 0x3 | 0x8);
      return v.toString(16);
    });
  }
}
