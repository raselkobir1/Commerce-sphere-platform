import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Header } from './layout/header';
import { ToastOutlet } from './core/toast';
import { ChatWidget } from './layout/chat-widget';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Header, ToastOutlet, ChatWidget],
  template: `
    <app-header />
    <router-outlet />
    <app-toast />
    <app-chat-widget />
  `,
})
export class App {}
