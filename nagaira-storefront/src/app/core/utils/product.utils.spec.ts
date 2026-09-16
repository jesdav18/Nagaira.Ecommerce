import { Product } from '../models/models';
import { getProductOfferPrice } from './product.utils';

describe('product offer pricing', () => {
  it('uses the quantity price before applying the offer', () => {
    const product = createProduct();
    expect(getProductOfferPrice(product, 10, 800)).toBe(64);
  });

  it('does not apply an offer below its minimum purchase', () => {
    const product = createProduct();
    product.applicableOffers![0].minPurchaseAmount = 900;
    expect(getProductOfferPrice(product, 10, 800)).toBeNull();
  });

  it('selects only the highest priority eligible offer', () => {
    const product = createProduct();
    product.applicableOffers!.push({
      id: 'lower-priority', offerType: 'Percentage', discountPercentage: 50,
      priority: 1, rules: []
    });
    expect(getProductOfferPrice(product, 1, 100)).toBe(80);
  });

  it('clamps a fixed discount at zero', () => {
    const product = createProduct();
    product.applicableOffers = [{
      id: 'fixed', offerType: 'FixedAmount', discountAmount: 150,
      priority: 1, rules: []
    }];
    expect(getProductOfferPrice(product, 1, 100)).toBe(0);
  });

  function createProduct(): Product {
    return {
      id: 'product-1', name: 'Producto', description: '', sku: 'SKU', slug: 'producto',
      isActive: true, categoryId: 'category-1', categoryName: 'Categoría',
      availableQuantity: 100, reservedQuantity: 0, hasVirtualStock: false,
      isFeatured: false, images: [], prices: [
        { id: 'retail', productId: 'product-1', priceLevelId: 'level', priceLevelName: 'Público', price: 100, priceWithoutTax: 86.21, minQuantity: 1, isActive: true },
        { id: 'bulk', productId: 'product-1', priceLevelId: 'level', priceLevelName: 'Público', price: 80, priceWithoutTax: 68.97, minQuantity: 10, isActive: true }
      ],
      applicableOffers: [{
        id: 'high-priority', offerType: 'Percentage', discountPercentage: 20,
        priority: 10, rules: []
      }]
    };
  }
});
