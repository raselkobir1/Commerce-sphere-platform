import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Header } from './layout/header';
import { ToastOutlet } from './core/toast';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Header, ToastOutlet],
  template: `
    <app-header />
    <router-outlet />
    <app-toast />
  `,
})
export class App {}
