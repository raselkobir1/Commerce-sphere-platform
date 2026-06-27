// The backend Product has a single `category` text field, so we model the storefront's
// category → sub-category tree here on the client. Each product's `category` value matches one
// of the sub-category names below; the parent grouping is purely a frontend concept.

export interface CategoryNode {
  name: string;
  icon: string; // simple emoji used in the sidebar
  subs: string[];
}

export const TAXONOMY: CategoryNode[] = [
  { name: 'Electronics', icon: '💻', subs: ['Smartphones', 'Laptops', 'Headphones', 'Cameras'] },
  { name: 'Fashion', icon: '👕', subs: ['T-Shirts', 'Shoes', 'Watches', 'Bags'] },
  { name: 'Home & Kitchen', icon: '🏠', subs: ['Furniture', 'Cookware', 'Decor'] },
  { name: 'Sports', icon: '⚽', subs: ['Fitness', 'Outdoor'] },
];

// Find the parent category name for a given sub-category (product.category value).
export function parentOf(sub: string): string | null {
  return TAXONOMY.find((c) => c.subs.includes(sub))?.name ?? null;
}
