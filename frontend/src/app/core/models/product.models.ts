// Mirrors the Product service DTOs.
export interface Product {
  id: string;
  name: string;
  description: string;
  sku: string;
  price: number;
  category: string;
  imageUrl: string | null;
  isActive: boolean;
  stock: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateProductRequest {
  name: string;
  description: string;
  sku: string;
  price: number;
  category: string;
  imageUrl?: string | null;
  initialStock: number;
}

export interface UpdateProductRequest {
  name: string;
  description: string;
  price: number;
  category: string;
  imageUrl?: string | null;
}
