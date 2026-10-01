import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'app-paginator',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './paginator.component.html',
  styleUrl: './paginator.component.scss',
})
export class PaginatorComponent {
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly totalPages = input.required<number>();
  readonly totalCount = input.required<number>();
  readonly pageChange = output<number>();
  readonly pageSizeChange = output<number>();

  protected readonly sizes = [10, 20, 50, 100];
}
