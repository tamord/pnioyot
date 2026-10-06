export type InquiryStatus = 'New' | 'InProgress' | 'Waiting' | 'Completed';
export type InquiryPriority = 'Low' | 'Medium' | 'High';

export interface Inquiry {
  id: string;
  title: string;
  organizationName: string;
  status: InquiryStatus;
  priority: InquiryPriority;
  assignedTo: string;
  createdAt: string;
  updatedAt: string;
  rowVersion: string;
}

export interface InquiryFilter {
  q?: string;
  status?: InquiryStatus | '';
  priority?: InquiryPriority | '';
  assignedTo?: string;
  organizationName?: string;
  sort?: string;
  page?: number;
  pageSize?: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface AssigneeCount {
  assignedTo: string;
  count: number;
}

export interface DashboardStats {
  total: number;
  byStatus: Record<string, number>;
  byPriority: Record<string, number>;
  topAssignees: AssigneeCount[];
  recent: Inquiry[];
}

export interface StatusUpdateResult {
  success: boolean;
  concurrencyConflict: boolean;
  error?: string;
  inquiry?: Inquiry;
}

export const STATUS_LABELS: Record<InquiryStatus, string> = {
  New: 'חדש',
  InProgress: 'בטיפול',
  Waiting: 'ממתין',
  Completed: 'הושלם'
};

export const PRIORITY_LABELS: Record<InquiryPriority, string> = {
  High: 'גבוהה',
  Medium: 'בינונית',
  Low: 'נמוכה'
};

export const STATUS_CLASS: Record<InquiryStatus, string> = {
  New: 'badge badge-new',
  InProgress: 'badge badge-progress',
  Waiting: 'badge badge-waiting',
  Completed: 'badge badge-done'
};

export const ALLOWED_TRANSITIONS: Record<InquiryStatus, InquiryStatus[]> = {
  New: ['InProgress', 'Waiting'],
  InProgress: ['Waiting', 'Completed'],
  Waiting: ['InProgress', 'Completed'],
  Completed: []
};

export function statusOptions(current: InquiryStatus): InquiryStatus[] {
  return [current, ...(ALLOWED_TRANSITIONS[current] ?? [])].filter(
    (status, index, all) => all.indexOf(status) === index
  );
}

export interface BulkStatusItem {
  id: string;
  rowVersion: string;
}

export interface BulkStatusFailure {
  id: string;
  code: 'NotFound' | 'Conflict' | 'Validation' | string;
  error: string;
  inquiry?: Inquiry;
}

export interface BulkStatusUpdateResult {
  mode: 'PartialSuccess' | string;
  requestedCount: number;
  succeededCount: number;
  failedCount: number;
  succeeded: Inquiry[];
  failed: BulkStatusFailure[];
}
