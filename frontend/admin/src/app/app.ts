import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Theme } from './core/theme';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: '<router-outlet />',
})
export class App {
  // Instantiating Theme on bootstrap applies the saved light/dark + accent before first render.
  private theme = inject(Theme);
}
