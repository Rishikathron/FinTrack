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

    // Load net worth
    this.valuationService.getNetWorth().subscribe({
      next: (data) => {
        this.summary.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load net worth data.');
        this.loading.set(false);
      }
    });

    // Load live prices
    this.priceService.getCurrentPrices().subscribe({
      next: (data) => this.prices.set(data),
      error: () => {} // Prices are optional on dashboard
    });
  }

  formatCurrency(value: number): string {
    return '₹' + value.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  }
}
