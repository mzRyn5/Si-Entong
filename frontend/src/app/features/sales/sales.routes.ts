import { Routes } from '@angular/router';
import { SalesListComponent } from './pages/sales-list/sales-list.component';
import { PosComponent } from './pages/pos/pos.component';
import { CashSessionComponent } from './pages/cash-session/cash-session.component';

export const SALES_ROUTES: Routes = [
  { path: '', component: SalesListComponent },
  { path: 'pos', component: PosComponent },
  { path: 'sessions', component: CashSessionComponent }
];
