export interface Category {
  id: number;
  name: string;
  description?: string | null;
  productCount: number;
}

export interface Product {
  id: number;
  sku: string;
  name: string;
  categoryId: number;
  categoryName: string;
  unitPrice: number;
  quantityOnHand: number;
  reorderLevel: number;
  isLowStock: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface StockTransaction {
  id: number;
  productId: number;
  productName: string;
  type: string;
  quantity: number;
  note?: string | null;
  createdAt: string;
}

export interface RecentTransaction {
  id: number;
  productName: string;
  type: string;
  quantity: number;
  createdAt: string;
}

export interface Dashboard {
  totalProducts: number;
  totalCategories: number;
  totalStockValue: number;
  lowStockCount: number;
  recentTransactions: RecentTransaction[];
}
