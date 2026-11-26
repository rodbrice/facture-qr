import type { InvoiceStatus } from '../api'
import { statusLabels } from '../format'

/** The state of an invoice, and "en retard" when the API says it is overdue. */
export function StatusBadge({ status, overdue }: { status: InvoiceStatus; overdue: boolean }) {
  return (
    <span className="status">
      <span className={`badge badge-${status.toLowerCase()}`}>{statusLabels[status]}</span>
      {overdue && <span className="badge badge-overdue">En retard</span>}
    </span>
  )
}
