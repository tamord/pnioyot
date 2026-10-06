import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { InquiryApiError } from '../models/api-error';
import {
  Inquiry,
  InquiryStatus,
  PRIORITY_LABELS,
  STATUS_LABELS,
  statusOptions
} from '../models/inquiry';
import { InquiryService } from '../services/inquiry.service';

@Component({
  selector: 'app-inquiry-detail',
  imports: [RouterLink, DatePipe, FormsModule],
  templateUrl: './inquiry-detail.component.html',
  styleUrl: './inquiry-detail.component.css'
})
export class InquiryDetailComponent implements OnInit {
  private readonly inquiries = inject(InquiryService);
  private readonly route = inject(ActivatedRoute);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly conflict = signal(false);
  readonly message = signal<string | null>(null);
  readonly inquiry = signal<Inquiry | null>(null);
  readonly statusLabels = STATUS_LABELS;
  readonly priorityLabels = PRIORITY_LABELS;
  readonly statuses = signal<InquiryStatus[]>([]);
  newStatus: InquiryStatus = 'New';

  ngOnInit(): void {
    this.load();
  }

  save(): void {
    const item = this.inquiry();
    if (!item) {
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    this.conflict.set(false);
    this.message.set(null);
    this.inquiries.updateStatus(item.id, this.newStatus, item.rowVersion).subscribe({
      next: (updated) => this.apply(updated, 'הסטטוס עודכן.'),
      error: (err: unknown) => {
        this.saving.set(false);
        if (err instanceof InquiryApiError && err.isConflict) {
          this.conflict.set(true);
          this.error.set(err.message);
          if (err.inquiry) {
            this.apply(err.inquiry);
          } else {
            this.load();
          }
          return;
        }

        this.error.set(err instanceof Error ? err.message : 'עדכון הסטטוס נכשל.');
      }
    });
  }

  private load(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set('מזהה פנייה חסר.');
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.inquiries.get(id).subscribe({
      next: (item) => {
        this.apply(item);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.error.set(err instanceof InquiryApiError && err.isNotFound ? 'הפנייה לא נמצאה.' : 'טעינת הפנייה נכשלה.');
        this.inquiry.set(null);
        this.loading.set(false);
      }
    });
  }

  private apply(item: Inquiry, message?: string): void {
    this.inquiry.set(item);
    this.newStatus = item.status;
    this.statuses.set(statusOptions(item.status));
    this.saving.set(false);
    if (message) {
      this.message.set(message);
    }
  }
}
