import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AssetService } from '../../services/asset';
import { Asset, AssetType } from '../../models/models';

@Component({
  selector: 'app-asset-list',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './asset-list.html',
  styleUrl: './asset-list.css',
})
export class AssetList implements OnInit {
  assets = signal<Asset[]>([]);
  loading = signal(true);
  error = signal('');
  successMessage = signal('');

  // Inline edit state
  editingId = signal<string | null>(null);
  editQuantity = signal(0);
  editRate = signal(0);
  editDate = signal('');
  // FD edit fields
  editAmount = signal(0);
  editInterestRate = signal(0);
  editTenureYears = signal(1);
  editTenureMonths = signal(0);
  editBank = signal('');
  editGoal = signal('');
  editNotes = signal('');
  saving = signal(false);

  // Inline add state
  addingCategory = signal<AssetType | null>(null);
  addQuantity = signal(0);
  addRate = signal(0);
  addAmount = signal(0);
  addDate = signal('');
  // FD add fields
  addInterestRate = signal(0);
  addTenureYears = signal(1);
  addTenureMonths = signal(0);
  addBank = signal('');
  addGoal = signal('');
  addNotes = signal('');
  submitting = signal(false);

  // Dropdown options
  yearOptions = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
  monthOptions = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];

  // Grouped assets by category
  goldAssets = computed(() => this.assets().filter((a: Asset) => a.type === AssetType.Gold));
  silverAssets = computed(() => this.assets().filter((a: Asset) => a.type === AssetType.Silver));
  fdAssets = computed(() => this.assets().filter((a: Asset) => a.type === AssetType.FD));

  // Category groups for template iteration
  categories = computed(() => [
    { type: AssetType.Gold, label: 'Gold (22K)', icon: '🥇', assets: this.goldAssets() },
    { type: AssetType.Silver, label: 'Silver', icon: '🥈', assets: this.silverAssets() },
    { type: AssetType.FD, label: 'Fixed Deposits', icon: '🏦', assets: this.fdAssets() },
  ]);

  constructor(private assetService: AssetService) {}

  ngOnInit(): void {
    this.loadAssets();
  }

  loadAssets(): void {
    this.loading.set(true);
    this.error.set('');

    this.assetService.getAssets().subscribe({
      next: (data: Asset[]) => {
        this.assets.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load assets.');
        this.loading.set(false);
      }
    });
  }

  // ─── Inline Add ───

  toggleAdd(type: AssetType): void {
    if (this.addingCategory() === type) {
      this.addingCategory.set(null);
    } else {
      this.addingCategory.set(type);
      this.addQuantity.set(0);
      this.addRate.set(0);
      this.addAmount.set(0);
      this.addDate.set('');
      this.addInterestRate.set(0);
      this.addTenureYears.set(1);
      this.addTenureMonths.set(0);
      this.addBank.set('');
      this.addGoal.set('');
      this.addNotes.set('');
      this.successMessage.set('');
      this.error.set('');
    }
  }

  isAdding(type: AssetType): boolean {
    return this.addingCategory() === type;
  }

  submitAdd(type: AssetType): void {
    this.submitting.set(true);
    this.successMessage.set('');
    this.error.set('');

    const isMetal = type === AssetType.Gold || type === AssetType.Silver;
    const tenure = this.addTenureYears() * 12 + this.addTenureMonths();

    this.assetService.addAsset({
      type,
      quantity: isMetal ? this.addQuantity() : 0,
      amount: !isMetal ? this.addAmount() : 0,
      purchaseDate: this.addDate() || undefined,
      purchaseRatePerGram: isMetal ? this.addRate() : 0,
      interestRate: !isMetal ? this.addInterestRate() : 0,
      tenureMonths: !isMetal ? tenure : 0,
      bankName: !isMetal ? this.addBank() : '',
      goal: !isMetal ? this.addGoal() : '',
      notes: !isMetal ? this.addNotes() : '',
    }).subscribe({
      next: () => {
        const label = type === AssetType.Gold ? 'Gold' : type === AssetType.Silver ? 'Silver' : 'FD';
        this.successMessage.set(`${label} asset added!`);
        this.addingCategory.set(null);
        this.submitting.set(false);
        this.loadAssets();
      },
      error: () => {
        this.error.set('Failed to add asset.');
        this.submitting.set(false);
      }
    });
  }

  // ─── Inline Edit ───

  startEdit(asset: Asset): void {
    this.editingId.set(asset.id);
    this.editQuantity.set(asset.type === 'FD' ? 0 : asset.quantity);
    this.editRate.set(asset.purchaseRatePerGram || 0);
    this.editDate.set(asset.purchaseDate ? asset.purchaseDate.substring(0, 10) : '');
    // FD fields
    this.editAmount.set(asset.amount || 0);
    this.editInterestRate.set(asset.interestRate || 0);
    this.editTenureYears.set(Math.floor((asset.tenureMonths || 0) / 12));
    this.editTenureMonths.set((asset.tenureMonths || 0) % 12);
    this.editBank.set(asset.bankName || '');
    this.editGoal.set(asset.goal || '');
    this.editNotes.set(asset.notes || '');
    this.error.set('');
    this.successMessage.set('');
  }

  cancelEdit(): void {
    this.editingId.set(null);
  }

  saveEdit(asset: Asset): void {
    this.saving.set(true);
    this.error.set('');

    const isFd = asset.type === 'FD';
    const tenure = this.editTenureYears() * 12 + this.editTenureMonths();

    this.assetService.updateAsset(asset.id, {
      quantity: isFd ? 0 : this.editQuantity(),
      amount: isFd ? this.editAmount() : 0,
      purchaseDate: this.editDate() || undefined,
      purchaseRatePerGram: this.editRate(),
      interestRate: isFd ? this.editInterestRate() : 0,
      tenureMonths: isFd ? tenure : 0,
      bankName: isFd ? this.editBank() : '',
      goal: isFd ? this.editGoal() : '',
      notes: isFd ? this.editNotes() : '',
    }).subscribe({
      next: (updated: Asset) => {
        this.assets.update((list: Asset[]) => list.map((a: Asset) => a.id === updated.id ? updated : a));
        this.editingId.set(null);
        this.saving.set(false);
        this.successMessage.set(`${asset.type} asset updated!`);
      },
      error: () => {
        this.error.set('Failed to update asset.');
        this.saving.set(false);
      }
    });
  }

  // ─── Delete ───

  deleteAsset(id: string): void {
    this.assetService.deleteAsset(id).subscribe({
      next: () => {
        this.assets.update((list: Asset[]) => list.filter((a: Asset) => a.id !== id));
        this.successMessage.set('Asset deleted.');
      },
      error: () => {
        this.error.set('Failed to delete asset.');
      }
    });
  }

  // ─── Helpers ───

  displayValue(asset: Asset): string {
    if (asset.type === 'FD') {
      return '₹' + asset.amount.toLocaleString('en-IN');
    }
    return asset.quantity + 'g';
  }

  formatCurrency(value: number): string {
    return '₹' + value.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  isEditing(assetId: string): boolean {
    return this.editingId() === assetId;
  }

  formatTenure(months: number): string {
    if (!months) return '—';
    if (months < 12) return `${months}mo`;
    const y = Math.floor(months / 12);
    const m = months % 12;
    return m > 0 ? `${y}y ${m}mo` : `${y}y`;
  }

  groupSummary(assets: Asset[], type: AssetType): string {
    if (type === AssetType.FD) {
      const total = assets.reduce((sum: number, a: Asset) => sum + a.amount, 0);
      return `${assets.length} deposit${assets.length !== 1 ? 's' : ''} · ₹${total.toLocaleString('en-IN')}`;
    }
    const totalGrams = assets.reduce((sum: number, a: Asset) => sum + a.quantity, 0);
    return `${assets.length} entr${assets.length !== 1 ? 'ies' : 'y'} · ${totalGrams}g`;
  }
}
