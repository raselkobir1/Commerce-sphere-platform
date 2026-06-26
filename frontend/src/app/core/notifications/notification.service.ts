import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

// Centralised, app-wide toasts. Keeps components free of snackbar wiring.
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly snackBar = inject(MatSnackBar);

  success(message: string): void {
    this.show(message, 'snack-success');
  }

  error(message: string): void {
    this.show(message, 'snack-error');
  }

  info(message: string): void {
    this.show(message, 'snack-info');
  }

  private show(message: string, panelClass: string): void {
    this.snackBar.open(message, 'Dismiss', {
      duration: 5000,
      horizontalPosition: 'right',
      verticalPosition: 'top',
      panelClass: [panelClass],
    });
  }
}
