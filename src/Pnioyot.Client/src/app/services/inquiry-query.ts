import { ParamMap, Params } from '@angular/router';
import { InquiryFilter, InquiryPriority, InquiryStatus } from '../models/inquiry';

export function filterFromParams(params: ParamMap): InquiryFilter {
  return {
    q: params.get('q') || undefined,
    status: (params.get('status') as InquiryStatus | null) || undefined,
    priority: (params.get('priority') as InquiryPriority | null) || undefined,
    organizationName: params.get('organizationName') || undefined,
    assignedTo: params.get('assignedTo') || undefined,
    sort: params.get('sort') || 'updated',
    page: Number(params.get('page') ?? 1),
    pageSize: Number(params.get('pageSize') ?? 25)
  };
}

export function filterToQueryParams(filter: InquiryFilter): Params {
  return {
    q: filter.q || null,
    status: filter.status || null,
    priority: filter.priority || null,
    organizationName: filter.organizationName || null,
    assignedTo: filter.assignedTo || null,
    sort: filter.sort && filter.sort !== 'updated' ? filter.sort : null,
    page: filter.page && filter.page !== 1 ? filter.page : null,
    pageSize: filter.pageSize && filter.pageSize !== 25 ? filter.pageSize : null
  };
}

export function sameFilter(a: InquiryFilter, b: InquiryFilter): boolean {
  return JSON.stringify(normalize(a)) === JSON.stringify(normalize(b));
}

function normalize(filter: InquiryFilter): InquiryFilter {
  return {
    q: filter.q || undefined,
    status: filter.status || undefined,
    priority: filter.priority || undefined,
    organizationName: filter.organizationName || undefined,
    assignedTo: filter.assignedTo || undefined,
    sort: filter.sort || 'updated',
    page: Number(filter.page || 1),
    pageSize: Number(filter.pageSize || 25)
  };
}
