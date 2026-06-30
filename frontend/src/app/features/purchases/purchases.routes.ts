import { Routes } from '@angular/router';
import { PurchasesListComponent } from './pages/purchases-list/purchases-list.component';
import { PurchaseCreateComponent } from './pages/purchase-create/purchase-create.component';

export const PURCHASES_ROUTES: Routes = [
  { path: '', component: PurchasesListComponent },
  { path: 'create', component: PurchaseCreateComponent }
];
