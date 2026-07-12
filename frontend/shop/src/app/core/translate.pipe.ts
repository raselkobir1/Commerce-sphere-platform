import { Pipe, PipeTransform, inject } from '@angular/core';
import { I18n } from './i18n';

// impure so it re-renders every change-detection pass when the language signal flips
@Pipe({ name: 't', pure: false })
export class TranslatePipe implements PipeTransform {
  private i18n = inject(I18n);

  transform(key: string, params?: Record<string, string | number>): string {
    return this.i18n.t(key, params);
  }
}
