import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ValuationService } from '../../services/valuation';
import { PriceService } from '../../services/price';
import { NetWorthSummary, MetalPrices } from '../../models/models';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit {
  Math = Math;

  summary = signal<NetWorthSummary | null>(null);
  prices = signal<MetalPrices | null>(null);
  loading = signal(true);
  error = signal('');

  constructor(
    private valuationService: ValuationService,
    private priceService: PriceService
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading.set(true);
    this.error.set('');

    this.valuationService.getNetWorth().subscribe({
      next: (data: NetWorthSummary) => {
        this.summary.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load net worth data.');
        this.loading.set(false);
      }
    });

    this.priceService.getCurrentPrices().subscribe({
      next: (data: MetalPrices) => this.prices.set(data),
      error: () => {}
    });
  }

  formatCurrency(value: number): string {
    return '₹' + value.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }

  formatGrams(value: number): string {
    return value.toLocaleString('en-IN', { minimumFractionDigits: 1, maximumFractionDigits: 2 }) + 'g';
  }

  formatKg(grams: number): string {
    return (grams / 1000).toLocaleString('en-IN', { minimumFractionDigits: 1, maximumFractionDigits: 2 }) + ' kg';
  }

  formatPercent(value: number): string {
    const abs = Math.abs(value);
    return (value >= 0 ? '+' : '-') + abs.toFixed(2) + '%';
  }
}
