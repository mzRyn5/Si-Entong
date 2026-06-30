import { ProductListItem } from '../../../core/models/master-data.model';

export interface PurchaseFormItem {
  productId: string;
  name: string;
  sku: string;
  quantity: number;
  unitPrice: number;
  subtotal: number;
  unitName: string;
  searchText: string;
  showDropdown: boolean;
  searchResults: ProductListItem[];
}

export interface ReturnItemForm {
  purchaseItemId: string;
  productId: string;
  productName: string;
  boughtQty: number;
  unitPrice: number;
  returnQty: number;
  returnAmount: number;
  selected: boolean;
}
