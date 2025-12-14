import { Product, ProductPrice } from '../models/models';

export function getProductPrice(product: Product, priceLevelId?: string): number {
  if (!product.prices || product.prices.length === 0) {
    return 0;
  }

  if (priceLevelId) {
    const priceForLevel = product.prices.find(p => p.priceLevelId === priceLevelId && p.isActive);
    if (priceForLevel) {
      return priceForLevel.price;
    }
  }

  const activePrices = product.prices.filter(p => p.isActive);
  if (activePrices.length === 0) {
    return 0;
  }

  const sortedPrices = activePrices.sort((a, b) => a.minQuantity - b.minQuantity);
  return sortedPrices[0].price;
}

export function getProductStock(product: Product): number {
  return product.availableQuantity || 0;
}

export function hasProductStock(product: Product): boolean {
  return getProductStock(product) > 0;
}

export function getPrimaryImage(product: Product): string {
  if (!product.images || product.images.length === 0) {
    return '/assets/placeholder.jpg';
  }
  
  const primary = product.images.find(img => img.isPrimary);
  return primary?.imageUrl || product.images[0].imageUrl || '/assets/placeholder.jpg';
}

