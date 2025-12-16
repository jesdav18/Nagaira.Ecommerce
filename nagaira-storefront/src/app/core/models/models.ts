export interface Product {
  id: string;
  name: string;
  description: string;
  sku: string;
  isActive: boolean;
  categoryId: string;
  categoryName: string;
  cost?: number;
  availableQuantity: number;
  reservedQuantity: number;
  hasVirtualStock: boolean;
  images: ProductImage[];
  prices: ProductPrice[];
}

export interface ProductImage {
  id: string;
  imageUrl: string;
  altText: string;
  isPrimary: boolean;
  displayOrder: number;
}

export interface ProductPrice {
  id: string;
  productId: string;
  priceLevelId: string;
  priceLevelName: string;
  price: number;
  minQuantity: number;
  isActive: boolean;
}

export interface PriceLevel {
  id: string;
  name: string;
  description: string;
  priority: number;
  markupPercentage: number;
  isActive: boolean;
}

export interface Category {
  id: string;
  name: string;
  description: string;
  slug: string;
  imageUrl?: string;
  isActive: boolean;
  parentCategoryId?: string;
  subCategories?: Category[];
}

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  role: string;
}

export interface AuthResponse {
  token: string;
  user: User;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
}

export interface Order {
  id: string;
  orderNumber: string;
  createdAt: string;
  subtotal: number;
  tax: number;
  shippingCost: number;
  total: number;
  status: string;
  items: OrderItem[];
  shippingAddress?: Address;
}

export interface OrderItem {
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  subtotal: number;
}

export interface Address {
  id: string;
  street: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  isDefault: boolean;
}

export interface CartItem {
  product: Product;
  quantity: number;
}

export interface CreateOrderRequest {
  items: Array<{
    productId: string;
    quantity: number;
  }>;
  shippingAddressId?: string;
}
