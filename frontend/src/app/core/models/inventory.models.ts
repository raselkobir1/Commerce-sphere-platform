// Mirrors the Inventory service InventoryItemResponse.
export interface InventoryItem {
  id: string;
  productId: string;
  sku: string;
  quantityOnHand: number;
  quantityReserved: number;
  quantityAvailable: number;
  reorderLevel: number;
  isActive: boolean;
}
