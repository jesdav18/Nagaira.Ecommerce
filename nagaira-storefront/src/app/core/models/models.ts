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
  priceWithoutTax: number;
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
  averageCost?: number;
  suppliers?: OrderItemSupplier[];
}

export interface OrderItemSupplier {
  productSupplierId: string;
  supplierName: string;
  quantity: number;
  unitCost: number;
  totalCost: number;
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

export interface Supplier {
  id: string;
  name: string;
  legalName?: string;
  taxId?: string;
  contactName?: string;
  email?: string;
  phone?: string;
  address?: string;
  city?: string;
  state?: string;
  country?: string;
  postalCode?: string;
  website?: string;
  notes?: string;
  paymentTerms?: string;
  leadTimeDays: number;
  minOrderAmount?: number;
  isActive: boolean;
  createdAt: string;
}

export interface ProductSupplier {
  id: string;
  productId: string;
  productName: string;
  supplierId: string;
  supplierName: string;
  supplierSku?: string;
  supplierCost: number;
  isPrimary: boolean;
  priority: number;
  leadTimeDays: number;
  minOrderQuantity: number;
  notes?: string;
  isActive: boolean;
  createdAt: string;
}

export interface SupplierCostHistory {
  id: string;
  productSupplierId: string;
  productName: string;
  supplierName: string;
  oldCost?: number;
  newCost: number;
  changedByUserName?: string;
  changeReason?: string;
  createdAt: string;
}

export interface Banner {
  id: string;
  title: string;
  subtitle?: string | null;
  imageUrl: string;
  linkUrl?: string | null;
  displayOrder: number;
  isActive: boolean;
  createdAt: string;
}
