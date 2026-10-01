import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

@Component({
  selector: 'app-paginator',
  imports: [DecimalPipe],
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
  protected readonly from = computed(() => (this.totalCount() === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1));
  protected readonly to = computed(() => Math.min(this.page() * this.pageSize(), this.totalCount()));
}
