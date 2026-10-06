import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { InquiryApiError } from '../models/api-error';
import {
  DashboardStats,
  Inquiry,
  InquiryFilter,
  PagedResult,
  BulkStatusItem,
  BulkStatusUpdateResult
} from '../models/inquiry';

@Injectable({ providedIn: 'root' })
export class InquiryService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/inquiries';

  search(filter: InquiryFilter): Observable<PagedResult<Inquiry>> {
    return this.http
      .get<PagedResult<Inquiry>>(this.baseUrl, { params: this.toParams(filter) })
      .pipe(catchError((err: HttpErrorResponse) => throwError(() => InquiryApiError.fromHttp(err))));
  }

  dashboard(): Observable<DashboardStats> {
    return this.http
      .get<DashboardStats>(`${this.baseUrl}/stats`)
      .pipe(catchError((err: HttpErrorResponse) => throwError(() => InquiryApiError.fromHttp(err))));
  }

  get(id: string): Observable<Inquiry> {
    return this.http
      .get<Inquiry>(`${this.baseUrl}/${id}`)
      .pipe(catchError((err: HttpErrorResponse) => throwError(() => InquiryApiError.fromHttp(err))));
  }

  updateStatus(id: string, status: Inquiry['status'], rowVersion: string): Observable<Inquiry> {
    return this.http
      .patch<Inquiry>(`${this.baseUrl}/${id}`, { status, rowVersion })
      .pipe(catchError((err: HttpErrorResponse) => throwError(() => InquiryApiError.fromHttp(err))));
  }

  bulkUpdateStatus(status: Inquiry['status'], items: BulkStatusItem[]): Observable<BulkStatusUpdateResult> {
    return this.http
      .patch<BulkStatusUpdateResult>(this.baseUrl, { status, items })
      .pipe(catchError((err: HttpErrorResponse) => throwError(() => InquiryApiError.fromHttp(err))));
  }

  exportUrl(filter: InquiryFilter): string {
    const params = this.toParams(filter).set('format', 'csv');
    return `${this.baseUrl}?${params.toString()}`;
  }

  private toParams(filter: InquiryFilter): HttpParams {
    let params = new HttpParams();
    const entries: [string, string | number | undefined][] = [
      ['q', filter.q],
      ['status', filter.status],
      ['priority', filter.priority],
      ['assignedTo', filter.assignedTo],
      ['organizationName', filter.organizationName],
      ['sort', filter.sort],
      ['page', filter.page],
      ['pageSize', filter.pageSize]
    ];

    for (const [key, value] of entries) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    }

    return params;
  }
}
