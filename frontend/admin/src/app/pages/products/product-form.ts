import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Api } from '../../core/api';
import { Category, Product } from '../../core/models';

// Used for both "new product" and "edit product". When an :id is in the URL we load and update;
// otherwise we create.
@Component({
  selector: 'app-product-form',
  imports: [FormsModule],
  template: `
    <div class="page-head"><h1>{{ id() ? 'Edit product' : 'New product' }}</h1></div>

    <form class="card card-pad" style="max-width:640px" (ngSubmit)="save()">
      <div class="field">
        <label>Name</label>
        <input class="input" name="name" [(ngModel)]="form.name" required />
      </div>

      <div class="field">
        <label>Description</label>
        <textarea name="description" rows="3" [(ngModel)]="form.description"></textarea>
      </div>

      <div class="row">
        <div class="field">
          <label>SKU</label>
          <input class="input" name="sku" [(ngModel)]="form.sku" [disabled]="!!id()" required />
        </div>
        <div class="field">
          <label>Category</label>
          <select name="category" [(ngModel)]="form.category" required>
            <option value="" disabled>Select a category…</option>
            @for (c of categories(); track c.id) {
              <option [value]="c.name">{{ c.name }}</option>
            }
            <!-- keep a legacy/inactive value selectable if the product already uses it -->
            @if (form.category && !categoryNames().includes(form.category)) {
              <option [value]="form.category">{{ form.category }} (current)</option>
            }
          </select>
          @if (categories().length === 0) {
            <div class="cell-sub" style="margin-top:6px">No categories yet — add some on the Categories page.</div>
          }
        </div>
      </div>

      <div class="row">
        <div class="field">
          <label>Price</label>
          <input class="input" type="number" name="price" [(ngModel)]="form.price" min="0" required />
        </div>
        @if (!id()) {
          <div class="field">
            <label>Initial stock</label>
            <input class="input" type="number" name="stock" [(ngModel)]="form.initialStock" min="0" />
          </div>
        }
      </div>

      <div class="field">
        <label>Image URL (optional)</label>
        <input class="input" name="imageUrl" [(ngModel)]="form.imageUrl" />
      </div>

      @if (error()) {
        <p class="error">{{ error() }}</p>
      }

      <div class="row" style="margin-top:8px">
        <button class="btn btn-primary" type="submit" [disabled]="loading()">
          {{ loading() ? 'Saving…' : 'Save' }}
        </button>
        <button class="btn" type="button" (click)="cancel()">Cancel</button>
      </div>
    </form>
  `,
})
export class ProductFormPage implements OnInit {
  private api = inject(Api);
  private router = inject(Router);

  id = input<string>(); // bound from the :id route param

  form = {
    name: '',
    description: '',
    sku: '',
    category: '',
    price: 0,
    initialStock: 0,
    imageUrl: '',
  };
  loading = signal(false);
  error = signal('');

  // Active categories for the dropdown (managed on the Categories page).
  categories = signal<Category[]>([]);
  categoryNames = computed(() => this.categories().map((c) => c.name));

  ngOnInit(): void {
    this.api.get<Category[]>('/api/categories').subscribe((c) => this.categories.set(c.filter((x) => x.isActive)));

    const id = this.id();
    if (id) {
      this.api.get<Product>(`/api/products/${id}`).subscribe((p) => {
        this.form = {
          name: p.name,
          description: p.description,
          sku: p.sku,
          category: p.category,
          price: p.price,
          initialStock: p.stock,
          imageUrl: p.imageUrl ?? '',
        };
      });
    }
  }

  save(): void {
    this.error.set('');
    this.loading.set(true);
    const id = this.id();

    const done = {
      next: () => this.router.navigate(['/products']),
      error: (err: { error?: { message?: string } }) => {
        this.loading.set(false);
        this.error.set(err?.error?.message ?? 'Could not save the product.');
      },
    };

    if (id) {
      this.api
        .put(`/api/products/${id}`, {
          name: this.form.name,
          description: this.form.description,
          price: this.form.price,
          category: this.form.category,
          imageUrl: this.form.imageUrl || null,
        })
        .subscribe(done);
    } else {
      this.api
        .post('/api/products', {
          name: this.form.name,
          description: this.form.description,
          sku: this.form.sku,
          price: this.form.price,
          category: this.form.category,
          imageUrl: this.form.imageUrl || null,
          initialStock: this.form.initialStock,
        })
        .subscribe(done);
    }
  }

  cancel(): void {
    this.router.navigate(['/products']);
  }
}
