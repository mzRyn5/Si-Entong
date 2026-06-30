import { ChangeDetectionStrategy, Component } from '@angular/core';
@Component({ selector: 'app-toast-container', standalone: true, templateUrl: './toast-container.component.html', styleUrl: './toast-container.component.scss', changeDetection: ChangeDetectionStrategy.OnPush })
export class ToastContainerComponent {}
