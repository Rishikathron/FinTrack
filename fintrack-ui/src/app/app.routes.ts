import { Routes } from '@angular/router';
import { Dashboard } from './components/dashboard/dashboard';
import { AssetList } from './components/asset-list/asset-list';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: Dashboard },
  { path: 'assets', component: AssetList }
];
