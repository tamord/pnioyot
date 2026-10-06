import { DatePipe, DecimalPipe, NgClass } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { EMPTY, Subject, debounceTime, distinctUntilChanged, merge, switchMap } from 'rxjs';
import { catchError, finalize, tap } from 'rxjs/operators';
import {
  BulkStatusUpdateResult,
  DashboardStats,
  Inquiry,
  InquiryFilter,
  InquiryPriority,
  InquiryStatus,
  PagedResult,
  PRIORITY_LABELS,
  STATUS_CLASS,
  STATUS_LABELS
} from '../models/inquiry';
import { filterFromParams, filterToQueryParams, sameFilter } from '../services/inquiry-query';
import { InquiryService } from '../services/inquiry.service';

@Component({
  selector: 'app-inquiry-list',
  imports: [ReactiveFormsModule, FormsModule, RouterLink, DatePipe, DecimalPipe, NgClass],
  templateUrl: './inquiry-list.component.html',
  styleUrl: './inquiry-list.component.css'
})
export class InquiryListComponent implements OnInit {
  private readonly inquiries = inject(InquiryService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reload$ = new Subject<void>();
  private allowFormNav = false;

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly result = signal<PagedResult<Inquiry> | null>(null);
  readonly aggregations = signal<DashboardStats | null>(null);
  readonly statusLabels = STATUS_LABELS;
  readonly priorityLabels = PRIORITY_LABELS;
  readonly statusClass = STATUS_CLASS;
  readonly statuses: InquiryStatus[] = ['New', 'InProgress', 'Waiting', 'Completed'];
  readonly priorities: InquiryPriority[] = ['High', 'Medium', 'Low'];

  readonly bulkStatus = signal<InquiryStatus>('InProgress');
  readonly bulkBusy = signal(false);
  readonly bulkResult = signal<BulkStatusUpdateResult | null>(null);
  readonly selectedIds = signal<Set<string>>(new Set());
  readonly selectedCount = computed(() => this.selectedIds().size);
  readonly allSelected = computed(() => {
    const items = this.result()?.items ?? [];
    return items.length > 0 && items.every((item) => this.selectedIds().has(item.id));
  });
  readonly empty = computed(() => {
    const data = this.result();
    return !this.loading() && !this.error() && !!data && data.items.length === 0;
  });

  readonly form = this.fb.nonNullable.group({
    q: '',
    status: '' as InquiryStatus | '',
    priority: '' as InquiryPriority | '',
    organizationName: '',
    assignedTo: '',
    sort: 'updated',
    pageSize: 25
  });

  ngOnInit(): void {
    this.inquiries.dashboard().subscribe({
      next: (stats) => this.aggregations.set(stats)
    });

    merge(this.route.queryParamMap, this.reload$)
      .pipe(
        tap(() => {
          this.syncForm(filterFromParams(this.route.snapshot.queryParamMap));
          this.allowFormNav = true;
        }),
        switchMap(() => {
          const filter = filterFromParams(this.route.snapshot.queryParamMap);
          this.loading.set(true);
          this.error.set(null);
          return this.inquiries.search(filter).pipe(
            catchError(() => {
              this.error.set('טעינת הפניות נכשלה. ודאו שה־API רץ ב־http://localhost:5080');
              return EMPTY;
            }),
            finalize(() => this.loading.set(false))
          );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((data) => {
        this.result.set(data);
        this.selectedIds.set(new Set());
      });

    merge(
      this.form.controls.q.valueChanges,
      this.form.controls.organizationName.valueChanges,
      this.form.controls.assignedTo.valueChanges
    )
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.applyFilters(true));

    merge(
      this.form.controls.status.valueChanges,
      this.form.controls.priority.valueChanges,
      this.form.controls.sort.valueChanges,
      this.form.controls.pageSize.valueChanges
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.applyFilters(false));
  }

  applyFilters(replaceUrl = false): void {
    if (!this.allowFormNav) {
      return;
    }

    const filter = this.formFilter(1);
    if (sameFilter(filter, filterFromParams(this.route.snapshot.queryParamMap))) {
      return;
    }

    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: filterToQueryParams(filter),
      replaceUrl
    });
  }

  clearFilters(): void {
    this.router.navigate([], { relativeTo: this.route, queryParams: {} });
  }

  goToPage(page: number): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: filterToQueryParams(this.formFilter(page))
    });
  }

  filterByStatus(status: InquiryStatus): void {
    this.form.controls.status.setValue(status);
  }

  exportCsv(): void {
    window.location.href = this.inquiries.exportUrl(this.formFilter());
  }

  retry(): void {
    this.reload$.next();
  }

  toggleAll(checked: boolean): void {
    const items = this.result()?.items ?? [];
    this.selectedIds.set(checked ? new Set(items.map((item) => item.id)) : new Set());
  }

  toggleOne(id: string, checked: boolean): void {
    this.selectedIds.update((current) => {
      const next = new Set(current);
      if (checked) {
        next.add(id);
      } else {
        next.delete(id);
      }
      return next;
    });
  }

  applyBulk(): void {
    const selected = this.selectedIds();
    const items = (this.result()?.items ?? [])
      .filter((item) => selected.has(item.id))
      .map((item) => ({ id: item.id, rowVersion: item.rowVersion }));

    if (items.length === 0) {
      return;
    }
    if (items.length > 100) {
      this.error.set('ניתן לעדכן עד 100 פניות בבקשה אחת.');
      return;
    }

    this.bulkBusy.set(true);
    this.bulkResult.set(null);
    this.inquiries.bulkUpdateStatus(this.bulkStatus(), items).subscribe({
      next: (result) => {
        this.bulkResult.set(result);
        this.bulkBusy.set(false);
        this.inquiries.dashboard().subscribe((stats) => this.aggregations.set(stats));
        this.reload$.next();
      },
      error: (err: Error) => {
        this.bulkBusy.set(false);
        this.error.set(err.message);
      }
    });
  }

  pages(): number[] {
    const data = this.result();
    if (!data) {
      return [];
    }
    const total = Math.max(1, data.totalPages);
    const start = Math.max(1, data.page - 2);
    const end = Math.min(total, start + 4);
    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  }

  agg(key: string): number {
    return this.aggregations()?.byStatus?.[key] ?? 0;
  }

  private syncForm(filter: InquiryFilter): void {
    this.form.patchValue(
      {
        q: filter.q ?? '',
        status: filter.status ?? '',
        priority: filter.priority ?? '',
        organizationName: filter.organizationName ?? '',
        assignedTo: filter.assignedTo ?? '',
        sort: filter.sort ?? 'updated',
        pageSize: Number(filter.pageSize ?? 25)
      },
      { emitEvent: false }
    );
  }

  private formFilter(page?: number): InquiryFilter {
    const value = this.form.getRawValue();
    return {
      q: value.q || undefined,
      status: value.status || undefined,
      priority: value.priority || undefined,
      organizationName: value.organizationName || undefined,
      assignedTo: value.assignedTo || undefined,
      sort: value.sort,
      page: page ?? Number(this.route.snapshot.queryParamMap.get('page') ?? 1),
      pageSize: Number(value.pageSize)
    };
  }
}
