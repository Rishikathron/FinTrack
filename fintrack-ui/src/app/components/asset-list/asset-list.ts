import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AssetService } from '../../services/asset';
import { Asset } from '../../models/models';

@Component({
  selector: 'app-asset-list',
  imports: [CommonModule, RouterLink],
  templateUrl: './asset-list.html',
  styleUrl: './asset-list.css',
})
export class AssetList implements OnInit {
  assets = signal<Asset[]>([]);
  loading = signal(true);
  error = signal('');

  constructor(private assetService: AssetService) {}

  ngOnInit(): void {
    this.loadAssets();
  }

  loadAssets(): void {
    this.loading.set(true);
    this.error.set('');

    this.assetService.getAssets().subscribe({
      next: (data) => {
        this.assets.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load assets.');
        this.loading.set(false);
      }
    });
  }

  deleteAsset(id: string): void {
    this.assetService.deleteAsset(id).subscribe({
      next: () => {
        // Remove from local list immediately
        this.assets.update(list => list.filter(a => a.id !== id));
      },
      error: () => {
        this.error.set('Failed to delete asset.');
      }
    });
  }

  /** Display the main value column depending on asset type. */
  displayValue(asset: Asset): string {
    if (asset.type === 'FD') {
      return '₹' + asset.amount.toLocaleString('en-IN');
    }
    return asset.quantity + ' grams';
  }
}
