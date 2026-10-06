import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DashboardStats, PRIORITY_LABELS, STATUS_LABELS } from '../models/inquiry';
import { InquiryService } from '../services/inquiry.service';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, DecimalPipe, DatePipe],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  private readonly inquiries = inject(InquiryService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly stats = signal<DashboardStats | null>(null);
  readonly statusLabels = STATUS_LABELS;
  readonly priorityLabels = PRIORITY_LABELS;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.inquiries.dashboard().subscribe({
      next: (data) => {
        this.stats.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.stats.set(null);
        this.error.set('לא ניתן לטעון את לוח הבקרה. ודאו שה־API רץ ב־http://localhost:5080');
        this.loading.set(false);
      }
    });
  }

  count(key: string): number {
    return this.stats()?.byStatus?.[key] ?? 0;
  }

  pcount(key: string): number {
    return this.stats()?.byPriority?.[key] ?? 0;
  }
}
