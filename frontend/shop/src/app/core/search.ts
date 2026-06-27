import { Injectable, signal } from '@angular/core';

// Shared search term so the header search box (any page) and the catalogue page stay in sync.
@Injectable({ providedIn: 'root' })
export class Search {
  term = signal('');
}
