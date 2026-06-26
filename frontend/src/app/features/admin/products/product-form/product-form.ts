import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Router, RouterLink } from '@angular/router';
import { NotificationService } from '../../../../core/notifications/notification.service';
import { ProductApiService } from '../../../storefront/data/product-api.service';

@Component({
  selector: 'app-product-form',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressBarModule,
  ],
  templateUrl: './product-form.html',
  styleUrl: './product-form.scss',
})
export class ProductForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ProductApiService);
  private readonly router = inject(Router);
  private readonly notify = inject(NotificationService);

  // Route param :id (edit mode) bound via withComponentInputBinding().
  @Input() id?: string;

  readonly loading = signal(false);
  readonly isEdit = signal(false);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.required]],
    sku: ['', [Validators.required]],
    price: [0, [Validators.required, Validators.min(0.01)]],
    category: ['', [Validators.required]],
    imageUrl: [''],
    initialStock: [0, [Validators.required, Validators.min(0)]],
  });

  ngOnInit(): void {
    if (this.id) {
      this.isEdit.set(true);
      // SKU and stock are immutable on update — disable them in edit mode.
      this.form.controls.sku.disable();
      this.form.controls.initialStock.disable();
      this.loading.set(true);
      this.api.getById(this.id).subscribe({
        next: (p) => {
          this.form.patchValue({
            name: p.name,
            description: p.description,
            sku: p.sku,
            price: p.price,
            category: p.category,
            imageUrl: p.imageUrl ?? '',
            initialStock: p.stock,
          });
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
    }
  }

  save(): void {
    if (this.form.invalid || this.loading()) return;
    this.loading.set(true);
    const v = this.form.getRawValue();

    const request$ = this.isEdit()
      ? this.api.update(this.id!, {
          name: v.name,
          description: v.description,
          price: v.price,
          category: v.category,
          imageUrl: v.imageUrl || null,
        })
      : this.api.create({
          name: v.name,
          description: v.description,
          sku: v.sku,
          price: v.price,
          category: v.category,
          imageUrl: v.imageUrl || null,
          initialStock: v.initialStock,
        });

    request$.subscribe({
      next: () => {
        this.loading.set(false);
        this.notify.success(this.isEdit() ? 'Product updated' : 'Product created');
        void this.router.navigateByUrl('/admin/products');
      },
      error: () => this.loading.set(false),
    });
  }
}
