import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AssetService } from '../../services/asset';
import { AssetType, AddAssetRequest } from '../../models/models';

@Component({
  selector: 'app-add-asset',
  imports: [CommonModule, FormsModule],
  templateUrl: './add-asset.html',
  styleUrl: './add-asset.css',
})
export class AddAsset {
  assetTypes = Object.values(AssetType);
  selectedType = signal<AssetType>(AssetType.Gold);
  quantity = signal(0);
  amount = signal(0);
  purchaseDate = signal('');
  purchaseRate = signal(0);
  submitting = signal(false);
  successMessage = signal('');
  errorMessage = signal('');

  constructor(private assetService: AssetService, private router: Router) {}

  /** True when the selected type is Gold or Silver (quantity-based). */
  get isMetalType(): boolean {
    return this.selectedType() === AssetType.Gold || this.selectedType() === AssetType.Silver;
  }

  onSubmit(): void {
    this.submitting.set(true);
    this.successMessage.set('');
    this.errorMessage.set('');

    const request: AddAssetRequest = {
      type: this.selectedType(),
      quantity: this.isMetalType ? this.quantity() : 0,
      amount: !this.isMetalType ? this.amount() : 0,
      purchaseDate: this.purchaseDate() || undefined,
      purchaseRatePerGram: this.isMetalType ? this.purchaseRate() : 0
    };

    this.assetService.addAsset(request).subscribe({
      next: () => {
        this.successMessage.set(`${this.selectedType()} asset added successfully!`);
        this.quantity.set(0);
        this.amount.set(0);
        this.purchaseDate.set('');
        this.purchaseRate.set(0);
        this.submitting.set(false);
      },
      error: () => {
        this.errorMessage.set('Failed to add asset. Please try again.');
        this.submitting.set(false);
      }
    });
  }
}
