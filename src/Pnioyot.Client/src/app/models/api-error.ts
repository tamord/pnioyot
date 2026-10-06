import { HttpErrorResponse } from '@angular/common/http';
import { Inquiry } from './inquiry';

export class InquiryApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly inquiry?: Inquiry
  ) {
    super(message);
    this.name = 'InquiryApiError';
  }

  get isConflict(): boolean {
    return this.status === 409;
  }

  get isNotFound(): boolean {
    return this.status === 404;
  }

  static fromHttp(err: HttpErrorResponse): InquiryApiError {
    const body = err.error;
    let message = 'הבקשה נכשלה.';
    let inquiry: Inquiry | undefined;

    if (body && typeof body === 'object') {
      const validation = body as { errors?: Record<string, string[]> };
      const firstError = validation.errors
        ? Object.values(validation.errors).flat()[0]
        : undefined;
      inquiry = (body as { inquiry?: Inquiry }).inquiry;
      message = firstError || (body as { error?: string }).error || message;
    }

    if (err.status === 404) {
      message = message === 'הבקשה נכשלה.' ? 'הפנייה לא נמצאה.' : message;
    }
    if (err.status === 409) {
      message =
        message === 'הבקשה נכשלה.'
          ? 'הפנייה עודכנה במקביל על ידי משתמש אחר. רעננו את המסך ונסו שוב.'
          : message;
    }
    if (err.status === 400 && message === 'הבקשה נכשלה.') {
      message = 'סטטוס אינו חוקי או שמעבר הסטטוס אינו מותר.';
    }

    return new InquiryApiError(message, err.status, inquiry);
  }
}
