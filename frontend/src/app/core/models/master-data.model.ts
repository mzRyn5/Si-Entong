export interface ProductListItem { id: string; sku: string; name: string; sellingPrice: number; currentStock: number; isActive: boolean; }
export interface ProductDetail {
  id: string;
  sku: string;
  barcode?: string;
  name: string;
  categoryId?: string;
  categoryName?: string;
  unitId: string;
  unitName?: string;
  purchasePrice: number;
  sellingPrice: number;
  currentStock: number;
  lowStockThreshold: number;
  isActive: boolean;
}
export interface CategoryItem { id: string; name: string; description?: string; isActive?: boolean; }
export interface UnitItem { id: string; name: string; description?: string; isActive?: boolean; }
export interface SupplierItem { id: string; name: string; isActive?: boolean; }
export interface CustomerItem { id: string; name: string; isActive?: boolean; }
