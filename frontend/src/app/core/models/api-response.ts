// Mirrors CommerceSphere.Shared.Common ApiResponse<T> — the envelope every endpoint returns.
export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[];
  traceId: string;
  correlationId: string;
}

// Mirrors PagedResult<T>.
export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalRecords: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
