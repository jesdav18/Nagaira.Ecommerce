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

function toPositiveNumber(value: unknown): number | null {
  if (typeof value !== 'number' || !Number.isFinite(value) || value <= 0) {
    return null;
  }

  return value;
}

export function getProductOfferPrice(product: Product, quantity = 1, cartBaseTotal?: number): number | null {
  const basePrice = getProductPriceByQuantity(product, quantity);
  const effectiveCartTotal = cartBaseTotal ?? basePrice * quantity;
  const configuredOffers = [...(product.applicableOffers ?? [])]
    .sort((a, b) => b.priority - a.priority || a.id.localeCompare(b.id));

  for (const offer of configuredOffers) {
    if (offer.minQuantity && quantity < offer.minQuantity) continue;
    if (offer.minPurchaseAmount && effectiveCartTotal < offer.minPurchaseAmount) continue;
    const itemSubtotal = basePrice * quantity;
    const rulesSatisfied = (offer.rules ?? []).every(rule => {
      switch ((rule.ruleType || '').trim().toLowerCase()) {
        case 'min_item_price': return basePrice >= rule.value;
        case 'max_item_price': return basePrice <= rule.value;
        case 'min_item_subtotal': return itemSubtotal >= rule.value;
        case 'max_item_subtotal': return itemSubtotal <= rule.value;
        case 'min_cart_total': return effectiveCartTotal >= rule.value;
        default: return false;
      }
    });
    if (!rulesSatisfied) continue;

    const requestedDiscount = offer.offerType === 'Percentage' && offer.discountPercentage
      ? basePrice * (offer.discountPercentage / 100)
      : offer.offerType === 'FixedAmount' && offer.discountAmount
        ? offer.discountAmount
        : 0;
    if (requestedDiscount > 0) return Math.max(basePrice - requestedDiscount, 0);
  }

  const directOfferPrice = toPositiveNumber(product.offerPrice ?? product.discountPrice);
  if (directOfferPrice !== null && (basePrice <= 0 || directOfferPrice < basePrice)) {
    return directOfferPrice;
  }

  const discountPercentage = toPositiveNumber(product.discountPercentage);
  if (basePrice > 0 && discountPercentage !== null) {
    return Math.max(basePrice - (basePrice * (discountPercentage / 100)), 0);
  }

  return null;
}

export function hasActiveOffer(product: Product): boolean {
  return getProductOfferPrice(product) !== null
    || toPositiveNumber(product.discountPercentage) !== null
    || product.hasOffer === true
    || product.isOnSale === true;
}

export function hasProductOffer(product: Product): boolean {
  return hasActiveOffer(product);
}

export function shouldShowOffer(product: Product): boolean {
  return hasActiveOffer(product) && getProductOfferPrice(product) !== null;
}

export function hasBulkPrice(product: Product): boolean {
  if (toPositiveNumber(product.bulkPrice ?? product.wholesalePrice ?? product.priceByQuantity) !== null) {
    return true;
  }

  if (typeof product.minimumQuantity === 'number' && product.minimumQuantity > 1) {
    return true;
  }

  return getWholesalePrice(product, false) !== null;
}

export function shouldShowBulkPrice(product: Product): boolean {
  return hasBulkPrice(product) && !hasActiveOffer(product);
}

export function getProductDisplayPriceByQuantity(
  product: Product,
  quantity: number,
  priceLevelId?: string,
  honorOffer = true
): number {
  return getProductPriceByQuantity(product, quantity, priceLevelId);
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

export function getWholesalePrice(product: Product, suppressWhenOffer = true): number | null {
  if (suppressWhenOffer && hasActiveOffer(product)) {
    return null;
  }

  const explicitPrice = toPositiveNumber(product.bulkPrice ?? product.wholesalePrice ?? product.priceByQuantity);
  if (explicitPrice !== null) {
    return explicitPrice;
  }

  if (typeof product.minimumQuantity === 'number' && product.minimumQuantity > 1) {
    const quantityPrice = getProductPriceByQuantity(product, product.minimumQuantity);
    const retailPrice = getProductPrice(product);
    if (quantityPrice > 0 && quantityPrice !== retailPrice) {
      return quantityPrice;
    }
  }

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

