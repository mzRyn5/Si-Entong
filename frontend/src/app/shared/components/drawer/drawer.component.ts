import { ChangeDetectionStrategy, Component } from '@angular/core';
@Component({ selector: 'app-drawer', standalone: true, templateUrl: './drawer.component.html', styleUrl: './drawer.component.scss', changeDetection: ChangeDetectionStrategy.OnPush })
export class DrawerComponent {}
