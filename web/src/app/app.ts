import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CurrentUserService } from './core/services/current-user.service';
import { RequestsPageComponent } from './features/requests/requests-page/requests-page.component';

@Component({
  selector: 'app-root',
  imports: [RequestsPageComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  protected readonly user = inject(CurrentUserService);
}
