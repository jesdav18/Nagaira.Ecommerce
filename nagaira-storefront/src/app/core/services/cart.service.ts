import { Injectable, inject, signal, computed } from '@angular/core';
import { CartItem, Product } from '../models/models';
import { getProductPrice } from '../utils/product.utils';
import { AppSettingsService } from './app-settings.service';

@Injectable({
  providedIn: 'root'
})
export class CartService {
  private appSettings = inject(AppSettingsService);
  private items = signal<CartItem[]>([]);

  cartItems = this.items.asReadonly();

  constructor() {
    this.loadCartFromStorage();
  }

  itemCount = computed(() =>
    this.items().reduce((total, item) => total + item.quantity, 0)
  );

  private grossSubtotal = computed(() =>
    this.items().reduce((total, item) => {
      const price = getProductPrice(item.product);
      return total + (price * item.quantity);
    }, 0)
  );

  subtotal = computed(() => {
    const rate = this.appSettings.taxRate();
    const divisor = 1 + rate;
    if (divisor <= 0) return this.grossSubtotal();
    return this.grossSubtotal() / divisor;
  });

  tax = computed(() => this.grossSubtotal() - this.subtotal());

  total = computed(() => this.grossSubtotal());

  addToCart(product: Product, quantity: number = 1): void {
    const currentItems = this.items();
    const existingItem = currentItems.find(item => item.product.id === product.id);

    if (existingItem) {
      const updatedItems = currentItems.map(item =>
        item.product.id === product.id
          ? { ...item, quantity: item.quantity + quantity }
          : item
      );
      this.items.set(updatedItems);
    } else {
      this.items.set([...currentItems, { product, quantity }]);
    }

    this.saveCartToStorage();
  }

  removeFromCart(productId: string): void {
    this.items.update(items => items.filter(item => item.product.id !== productId));
    this.saveCartToStorage();
  }

  updateQuantity(productId: string, quantity: number): void {
    if (quantity <= 0) {
      this.removeFromCart(productId);
      return;
    }

    this.items.update(items =>
      items.map(item =>
        item.product.id === productId ? { ...item, quantity } : item
      )
    );
    this.saveCartToStorage();
  }

  clearCart(): void {
    this.items.set([]);
    localStorage.removeItem('cart');
  }

  refreshTaxRate(): void {
    this.appSettings.reloadSettings();
  }

  private saveCartToStorage(): void {
    localStorage.setItem('cart', JSON.stringify(this.items()));
  }

  private loadCartFromStorage(): void {
    const cartStr = localStorage.getItem('cart');
    if (cartStr) {
      try {
        const cart = JSON.parse(cartStr);
        this.items.set(cart);
      } catch (e) {
        console.error('Error loading cart from storage', e);
      }
    }
  }
}
