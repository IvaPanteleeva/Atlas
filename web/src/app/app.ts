import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';

@Component({
    imports: [RouterOutlet, NzButtonModule, NzIconModule],
    selector: 'atlas-root',
    styleUrl: './app.scss',
    templateUrl: './app.html',
})
export class App {}
