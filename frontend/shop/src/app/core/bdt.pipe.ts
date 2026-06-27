import { formatNumber } from '@angular/common';
import { Pipe, PipeTransform } from '@angular/core';

// Formats a number as Bangladeshi Taka, e.g. 1299.5 -> "৳1,299.50".
// One place to change the currency for the whole storefront.
@Pipe({ name: 'bdt' })
export class BdtPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    return '৳' + formatNumber(Number(value ?? 0), 'en-US', '1.2-2');
  }
}
