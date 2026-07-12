import { Component, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Orders } from '../../core/orders';
import { BdtPipe } from '../../core/bdt.pipe';
import { TranslatePipe } from '../../core/translate.pipe';
import { I18n } from '../../core/i18n';

@Component({
  selector: 'app-order-success',
  imports: [BdtPipe, RouterLink, TranslatePipe],
  template: `
    <div class="container">
      @if (orders.last(); as o) {
        <div class="success">
          <div class="check">✓</div>
          <h1>{{ 'os.thankYou' | t }}</h1>
          <p class="muted">{{ 'os.paidCodPrefix' | t }} <strong>{{ 'checkout.cod' | t }}</strong>.</p>
          <div class="ref">{{ i18n.t('os.orderRef', { ref: o.reference }) }}</div>

          <div class="panel" style="text-align:left">
            <h2>{{ 'os.deliveryTo' | t }}</h2>
            <p style="margin:0">
              {{ o.address.fullName }}<br />
              {{ o.address.line1 }}, {{ o.address.city }} {{ o.address.postcode }}<br />
              📞 {{ o.address.phone }}
            </p>
            <p class="muted" style="margin-top:8px">{{ i18n.t('os.placed', { date: placedDate(o.placedAt) }) }}</p>

            <div class="order-lines">
              @for (item of o.items; track item.id) {
                <div class="l"><span>{{ item.productName }} × {{ item.quantity }}</span><span>{{ item.lineTotal | bdt }}</span></div>
              }
              <div class="l" style="font-weight:800;border-top:1px solid var(--line);margin-top:6px;padding-top:10px">
                <span>{{ 'os.totalPayOnDelivery' | t }}</span><span>{{ o.total | bdt }}</span>
              </div>
            </div>
          </div>

          <a class="btn btn-primary" routerLink="/">{{ 'cart.continueShopping' | t }}</a>
        </div>
      } @else {
        <div class="empty">{{ 'os.noRecentOrder' | t }} <a class="btn-ghost" routerLink="/">{{ 'os.goShopping' | t }}</a></div>
      }
    </div>
  `,
})
export class OrderSuccessPage {
  orders = inject(Orders);
  i18n = inject(I18n);
  private datePipe = new DatePipe('en-US');

  placedDate(d: Date): string {
    return this.datePipe.transform(d, 'medium') ?? '';
  }
}
