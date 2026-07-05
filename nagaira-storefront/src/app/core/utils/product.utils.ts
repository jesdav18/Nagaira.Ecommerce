import { Product, ProductPrice } from '../models/models';

const productNameCollator = new Intl.Collator('es', {
  sensitivity: 'base',
  numeric: true
});

export function sortProductsByName(products: Product[]): Product[] {
  return [...products].sort((a, b) =>
    productNameCollator.compare((a.name || '').trim(), (b.name || '').trim())
  );
}

export function getProductPrice(product: Product, priceLevelId?: string): number {
  return getProductPriceByQuantity(product, 1, priceLevelId);
}

export function getProductPriceByQuantity(product: Product, quantity: number, priceLevelId?: string): number {
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

  const matchedPrice = [...activePrices]
    .sort((a, b) => b.minQuantity - a.minQuantity)
    .find(price => quantity >= price.minQuantity);

  return matchedPrice?.price ?? getRetailPrice(activePrices);
}

export function getWholesalePrice(product: Product): number | null {
  if (!product.prices || product.prices.length === 0) {
    return null;
  }

  const wholesale = product.prices.find(p =>
    p.isActive && (p.priceLevelName || '').trim().toLowerCase().includes('mayorista')
  );

  return wholesale ? wholesale.price : null;
}

function getRetailPrice(activePrices: ProductPrice[]): number {
  const sortedPrices = [...activePrices].sort((a, b) => a.minQuantity - b.minQuantity);
  return sortedPrices[0].price;
}

export function getProductStock(product: Product): number | null {
  if (product.hasVirtualStock) {
    return null;
  }
  return product.availableQuantity || 0;
}

export function hasProductStock(product: Product): boolean {
  if (product.hasVirtualStock) {
    return true;
  }
  return getProductStock(product) !== null && (getProductStock(product) ?? 0) > 0;
}

export function isVirtualStock(product: Product): boolean {
  return product.hasVirtualStock;
}

export function getPrimaryImage(product: Product): string {
  if (!product.images || product.images.length === 0) {
    return '/assets/placeholder.jpg';
  }
  
  const primary = product.images.find(img => img.isPrimary);
  return primary?.imageUrl || product.images[0].imageUrl || '/assets/placeholder.jpg';
}

