import { Routes } from '@angular/router';
import { Dashboard } from './components/dashboard/dashboard';
import { AddAsset } from './components/add-asset/add-asset';
import { AssetList } from './components/asset-list/asset-list';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: Dashboard },
  { path: 'add-asset', component: AddAsset },
  { path: 'assets', component: AssetList }
];
