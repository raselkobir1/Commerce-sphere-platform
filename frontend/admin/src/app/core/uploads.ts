import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { API_URL } from './api';

interface Envelope<T> {
  success: boolean;
  message: string;
  data: T;
  errors: string[];
}

// Uploads image files to the Product service (which forwards them to Cloudinary) and returns the
// hosted URL. Uses HttpClient directly because the body is multipart/form-data, not JSON; the auth
// interceptor still attaches the bearer token automatically.
@Injectable({ providedIn: 'root' })
export class Uploads {
  private http = inject(HttpClient);

  uploadImage(file: File): Observable<string> {
    const form = new FormData();
    form.append('file', file);
    return this.http
      .post<Envelope<{ url: string }>>(`${API_URL}/api/products/images`, form)
      .pipe(map((r) => r.data.url));
  }
}
