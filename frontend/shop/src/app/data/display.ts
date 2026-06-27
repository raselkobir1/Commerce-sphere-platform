// Deterministic "rating" + review count derived from a product's id/sku, so the demo store shows
// stable star ratings without a real reviews backend.

function hash(s: string): number {
  let h = 0;
  for (const ch of s) h = (h * 31 + ch.charCodeAt(0)) >>> 0;
  return h;
}

export function ratingFor(seed: string): number {
  return Math.round((3.6 + (hash(seed) % 14) / 10) * 10) / 10; // 3.6 – 4.9
}

export function reviewsFor(seed: string): number {
  return 12 + (hash(seed) % 488);
}

export function stars(rating: number): string {
  const full = Math.round(rating);
  return '★'.repeat(full) + '☆'.repeat(5 - full);
}
