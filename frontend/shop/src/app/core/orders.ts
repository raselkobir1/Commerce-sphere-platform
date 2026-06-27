import { Injectable, signal } from '@angular/core';
import { PlacedOrder } from './models';

// Holds the most recently placed order so the confirmation page can render it. (The backend
// checkout endpoint stores no address/order detail, so we keep it in memory for the demo.)
@Injectable({ providedIn: 'root' })
export class Orders {
  last = signal<PlacedOrder | null>(null);
}
