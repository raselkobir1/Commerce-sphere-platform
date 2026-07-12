import { Injectable, signal } from '@angular/core';

// Shared show/hide state for the catalog page's category sidebar, toggled from the
// header (next to the logo) and read by the catalog page's layout.
@Injectable({ providedIn: 'root' })
export class CatalogUi {
  filtersOpen = signal(true);
}
