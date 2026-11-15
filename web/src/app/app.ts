import { Component } from '@angular/core';
import { ShellComponent } from './libs/shared/layout/shell/shell.component';

@Component({
    imports: [ShellComponent],
    selector: 'atlas-root',
    templateUrl: './app.html',
})
export class App {}
