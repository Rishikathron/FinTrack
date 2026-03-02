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
  purchaseDate: string;
  purchaseRatePerGram: number;
  // FD-specific
  interestRate: number;
  tenureMonths: number;
  bankName: string;
  goal: string;
  notes: string;
  createdAt: string;
}

export interface AddAssetRequest {
  type: AssetType;
  quantity: number;
  amount: number;
  purchaseDate?: string;
  purchaseRatePerGram: number;
  // FD-specific
  interestRate?: number;
  tenureMonths?: number;
  bankName?: string;
  goal?: string;
  notes?: string;
}

export interface UpdateAssetRequest {
  quantity: number;
  amount: number;
  purchaseDate?: string;
  purchaseRatePerGram: number;
  // FD-specific
  interestRate?: number;
  tenureMonths?: number;
  bankName?: string;
  goal?: string;
  notes?: string;
}

export interface MetalPrices {
  goldPricePerGram: number;
  silverPricePerGram: number;
  goldDailyChangePercent: number;
  silverDailyChangePercent: number;
  fetchedAt: string;
}

export interface FDBankSummary {
  bankName: string;
  count: number;
  principal: number;
  currentValue: number;
  accruedInterest: number;
}

export interface NetWorthSummary {
  goldValue: number;
  silverValue: number;
  fdValue: number;
  totalNetWorth: number;

  goldTotalGrams: number;
  goldSovereigns: number;
  goldInvested: number;
  goldProfitLoss: number;
  goldProfitLossPercent: number;

  silverTotalGrams: number;
  silverInvested: number;
  silverProfitLoss: number;
  silverProfitLossPercent: number;

  fdPrincipal: number;
  fdAccruedInterest: number;
  fdCount: number;
  fdBankBreakdown: FDBankSummary[];

  totalInvested: number;
  totalProfitLoss: number;
  totalProfitLossPercent: number;
}

export interface AssetValuation {
  assetId: string;
  type: AssetType;
  quantity: number;
  pricePerUnit: number;
  totalValue: number;
  purchaseDate: string;
  purchaseRatePerGram: number;
  purchaseValue: number;
  profitLoss: number;
  profitLossPercent: number;
  // FD-specific
  interestRate: number;
  accruedInterest: number;
  tenureMonths: number;
  maturityDate: string;
  bankName: string;
  goal: string;
  notes: string;
}

export interface ChatRequest {
  message: string;
}

export interface ChatResponse {
  reply: string;
  dataChanged: boolean;
}

export interface ChatMessage {
  role: 'user' | 'ai';
  text: string;
}
