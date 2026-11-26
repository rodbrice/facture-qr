import { useCallback, useState } from 'react'
import { Link, useParams } from 'react-router'
import { api, type Invoice, type IssueDates, type NewLine } from '../api'
import { FormAlert } from '../components/FormAlert'
import { LoadState } from '../components/LoadState'
import { StatusBadge } from '../components/StatusBadge'
import { usePageTitle } from '../components/usePageTitle'
import { useResource } from '../components/useResource'
import { IssueForm } from '../forms/IssueForm'
import { LineForm } from '../forms/LineForm'
import { MarkPaidForm } from '../forms/MarkPaidForm'
import { formatChf, formatDate, streetLine, townLine } from '../format'
import { formErrors } from '../formErrors'

/**
 * One invoice: the editor of a draft (lines, issue), then the PDF and the payment. Every amount
 * shown comes from the API's answer; the page never computes a total itself.
 */
export function InvoicePage() {
  const { id = '' } = useParams()
  const load = useCallback(() => api.invoice(id), [id])
  const { data: invoice, loading, error, setData } = useResource(load)
  const [notice, setNotice] = useState('')
  const [lineErrors, setLineErrors] = useState<string[]>([])
  usePageTitle(invoice ? title(invoice) : 'Facture')

  async function addLine(line: NewLine) {
    setData(await api.addLine(id, line))
    setNotice('Ligne ajoutée.')
  }

  async function removeLine(lineId: string, position: number) {
    try {
      setData(await api.removeLine(id, lineId))
      setLineErrors([])
      setNotice(`Ligne ${position} supprimée.`)
    } catch (e) {
      setLineErrors(formErrors(e, []).form)
    }
  }

  async function issue(dates: IssueDates) {
    const issued = await api.issue(id, dates)
    setData(issued)
    setNotice(`Facture émise sous le numéro ${issued.number}.`)
  }

  async function markPaid(paidOn?: string) {
    const paid = await api.markPaid(id, paidOn)
    setData(paid)
    setNotice(`Paiement enregistré le ${formatDate(paid.paidOn)}.`)
  }

  return (
    <>
      <p>
        <Link to="/factures">← Factures</Link>
      </p>
      <LoadState loading={loading && !invoice} error={error} />
      {invoice && (
        <>
          <div className="page-head">
            <h1>{title(invoice)}</h1>
            <StatusBadge status={invoice.status} overdue={invoice.overdue} />
          </div>
          <p role="status">{notice}</p>

          <div className="invoice-head">
            <section aria-labelledby="bill-to-title">
              <h2 id="bill-to-title">Adressée à</h2>
              <address>
                {invoice.billTo.name}
                <br />
                {streetLine(invoice.billTo) && (
                  <>
                    {streetLine(invoice.billTo)}
                    <br />
                  </>
                )}
                {townLine(invoice.billTo)}
                <br />
                {invoice.billTo.country}
              </address>
            </section>
            {invoice.issueDate && (
              <dl>
                <dt>Émise le</dt>
                <dd>{formatDate(invoice.issueDate)}</dd>
                <dt>Échéance</dt>
                <dd>{formatDate(invoice.dueDate)}</dd>
                {invoice.paidOn && (
                  <>
                    <dt>Payée le</dt>
                    <dd>{formatDate(invoice.paidOn)}</dd>
                  </>
                )}
              </dl>
            )}
          </div>

          <h2>Lignes</h2>
          {invoice.lines.length === 0 ? (
            <p>Aucune ligne. Ajoutez-en au moins une pour pouvoir émettre la facture.</p>
          ) : (
            <div className="table-scroll">
              <table>
                <caption className="visually-hidden">Lignes de la facture et total</caption>
                <thead>
                  <tr>
                    <th scope="col">N°</th>
                    <th scope="col">Description</th>
                    <th scope="col" className="amount">
                      Quantité
                    </th>
                    <th scope="col" className="amount">
                      Prix unitaire (CHF)
                    </th>
                    <th scope="col" className="amount">
                      Montant (CHF)
                    </th>
                    {invoice.status === 'Draft' && (
                      <th scope="col">
                        <span className="visually-hidden">Actions</span>
                      </th>
                    )}
                  </tr>
                </thead>
                <tbody>
                  {invoice.lines.map((line) => (
                    <tr key={line.id}>
                      <td>{line.position}</td>
                      <td>{line.description}</td>
                      <td className="amount">{line.quantity}</td>
                      <td className="amount">{formatChf(line.unitPrice)}</td>
                      <td className="amount">{formatChf(line.amount)}</td>
                      {invoice.status === 'Draft' && (
                        <td>
                          <button type="button" className="secondary" onClick={() => removeLine(line.id, line.position)} aria-label={`Supprimer la ligne ${line.position}`}>
                            Supprimer
                          </button>
                        </td>
                      )}
                    </tr>
                  ))}
                </tbody>
                <tfoot>
                  <tr>
                    <th scope="row" colSpan={4}>
                      Total (CHF)
                    </th>
                    <td className="amount total" data-testid="invoice-total">
                      {formatChf(invoice.total)}
                    </td>
                  </tr>
                </tfoot>
              </table>
            </div>
          )}
          <FormAlert messages={lineErrors} />

          {invoice.status === 'Draft' && (
            <div className="actions">
              <LineForm onAdd={addLine} />
              <IssueForm onIssue={issue} />
            </div>
          )}

          {invoice.status !== 'Draft' && (
            <div className="actions">
              <section className="panel" aria-labelledby="pdf-title">
                <h3 id="pdf-title">PDF avec QR-facture</h3>
                <p>
                  <a className="button" href={api.pdfUrl(invoice.id)} download>
                    Télécharger le PDF
                  </a>
                </p>
              </section>
              {invoice.status === 'Issued' && <MarkPaidForm onMarkPaid={markPaid} />}
            </div>
          )}
        </>
      )}
    </>
  )
}

function title(invoice: Invoice): string {
  return invoice.number ? `Facture ${invoice.number}` : 'Brouillon de facture'
}
