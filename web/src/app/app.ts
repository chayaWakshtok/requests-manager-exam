import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CurrentUserService } from './core/services/current-user.service';
import { RequestsPageComponent } from './features/requests/requests-page.component';

@Component({
  selector: 'app-root',
  imports: [RequestsPageComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="top">
      <h1>ניהול פניות</h1>
      <label>
        משתמש נוכחי
        <input [value]="user.name()" (change)="user.setName($any($event.target).value)" />
      </label>
    </header>
    <main>
      <app-requests-page />
    </main>
  `,
  styles: `
    .top { display: flex; justify-content: space-between; align-items: center; padding: 12px 24px; background: #1f2937; color: #fff; }
    h1 { font-size: 20px; margin: 0; }
    label { display: flex; gap: 8px; align-items: center; font-size: 13px; }
    input { padding: 4px 8px; border-radius: 4px; border: 0; }
    main { padding: 16px 24px; }
  `,
})
export class App {
  protected readonly user = inject(CurrentUserService);
}
