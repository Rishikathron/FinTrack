export enum AssetType {
  Gold = 'Gold',
  Silver = 'Silver',
  FD = 'FD'
}

export interface Asset {
  id: string;
  type: AssetType;
  quantity: number;
  amount: number;
  unit: string;
  createdAt: string;
}

export interface AddAssetRequest {
  type: AssetType;
  quantity: number;
  amount: number;
}

export interface MetalPrices {
  goldPricePerGram: number;
  silverPricePerGram: number;
  fetchedAt: string;
}

export interface NetWorthSummary {
  goldValue: number;
  silverValue: number;
  fdValue: number;
  totalNetWorth: number;
}

export interface AssetValuation {
  assetId: string;
  type: AssetType;
  quantity: number;
  pricePerUnit: number;
  totalValue: number;
}
