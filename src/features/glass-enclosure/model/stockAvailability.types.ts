export interface StockAvailabilityRow {
  bomLineId: string;
  productId: string | null;
  productSku: string;
  productName: string;
  requiredQty: number;
  availableQty: number;
  shortageQty: number;
  hasShortage: boolean;
  isService: boolean;
  warehouseId: string | null;
  substitutes: StockAvailabilitySubstitute[];
}

export interface StockAvailabilitySubstitute {
  productId: string;
  productSku: string;
  productName: string;
  availableQty: number;
  conversionRate: number;
  depth: number;
}

export interface BomShortageDto {
  bomLineId: string;
  productId: string;
  productSku: string;
  requiredQty: number;
  availableQty: number;
  shortageQty: number;
  substituteCount: number;
}
