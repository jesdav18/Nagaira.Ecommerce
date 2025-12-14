import { Routes } from '@angular/router';
import { adminGuard } from './core/guards/admin.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent)
  },
  {
    path: 'products',
    loadComponent: () => import('./features/products/products.component').then(m => m.ProductsComponent)
  },
  {
    path: 'categories',
    loadComponent: () => import('./features/categories/categories.component').then(m => m.CategoriesComponent)
  },
  {
    path: 'products/:id',
    loadComponent: () => import('./features/product-detail/product-detail.component').then(m => m.ProductDetailComponent)
  },
  {
    path: 'cart',
    loadComponent: () => import('./features/cart/cart.component').then(m => m.CartComponent)
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register.component').then(m => m.RegisterComponent)
  },
  {
    path: 'checkout',
    loadComponent: () => import('./features/checkout/checkout.component').then(m => m.CheckoutComponent)
  },
  {
    path: 'orders',
    loadComponent: () => import('./features/orders/order-list.component').then(m => m.OrderListComponent)
  },
  {
    path: 'profile',
    loadComponent: () => import('./features/profile/profile.component').then(m => m.ProfileComponent)
  },
  {
    path: 'admin',
    loadComponent: () => import('./features/admin/admin-layout/admin-layout.component').then(m => m.AdminLayoutComponent),
    canActivate: [adminGuard],
    children: [
      {
        path: '',
        loadComponent: () => import('./features/admin/admin-dashboard/admin-dashboard.component').then(m => m.AdminDashboardComponent)
      },
      {
        path: 'products',
        loadComponent: () => import('./features/admin/admin-products/admin-products.component').then(m => m.AdminProductsComponent)
      },
      {
        path: 'products/new',
        loadComponent: () => import('./features/admin/admin-products/admin-product-form/admin-product-form.component').then(m => m.AdminProductFormComponent)
      },
      {
        path: 'products/:id',
        loadComponent: () => import('./features/admin/admin-products/admin-product-form/admin-product-form.component').then(m => m.AdminProductFormComponent)
      },
      {
        path: 'kardex',
        loadComponent: () => import('./features/admin/admin-kardex/admin-kardex.component').then(m => m.AdminKardexComponent)
      },
      {
        path: 'kardex/:id',
        loadComponent: () => import('./features/admin/admin-kardex/admin-kardex-detail/admin-kardex-detail.component').then(m => m.AdminKardexDetailComponent)
      },
      {
        path: 'offers',
        loadComponent: () => import('./features/admin/admin-offers/admin-offers.component').then(m => m.AdminOffersComponent)
      },
      {
        path: 'reports',
        loadComponent: () => import('./features/admin/admin-reports/admin-reports.component').then(m => m.AdminReportsComponent)
      },
      {
        path: 'audit',
        loadComponent: () => import('./features/admin/admin-audit/admin-audit.component').then(m => m.AdminAuditComponent)
      },
      {
        path: 'categories',
        loadComponent: () => import('./features/admin/admin-categories/admin-categories.component').then(m => m.AdminCategoriesComponent)
      },
      {
        path: 'categories/new',
        loadComponent: () => import('./features/admin/admin-categories/admin-category-form/admin-category-form.component').then(m => m.AdminCategoryFormComponent)
      },
      {
        path: 'categories/:id',
        loadComponent: () => import('./features/admin/admin-categories/admin-category-form/admin-category-form.component').then(m => m.AdminCategoryFormComponent)
      },
      {
        path: 'payment-methods',
        loadComponent: () => import('./features/admin/admin-payment-methods/admin-payment-methods.component').then(m => m.AdminPaymentMethodsComponent)
      },
      {
        path: 'payment-methods/new',
        loadComponent: () => import('./features/admin/admin-payment-methods/admin-payment-method-form/admin-payment-method-form.component').then(m => m.AdminPaymentMethodFormComponent)
      },
      {
        path: 'payment-methods/:id',
        loadComponent: () => import('./features/admin/admin-payment-methods/admin-payment-method-form/admin-payment-method-form.component').then(m => m.AdminPaymentMethodFormComponent)
      },
      {
        path: 'settings',
        loadComponent: () => import('./features/admin/admin-settings/admin-settings.component').then(m => m.AdminSettingsComponent)
      }
    ]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
