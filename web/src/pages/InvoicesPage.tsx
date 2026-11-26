import { Link, useNavigate } from 'react-router'
import { api } from '../api'
import { LoadState } from '../components/LoadState'
import { StatusBadge } from '../components/StatusBadge'
import { usePageTitle } from '../components/usePageTitle'
import { useResource } from '../components/useResource'
import { NewDraftForm } from '../forms/NewDraftForm'
import { formatChf, formatDate } from '../format'

export function InvoicesPage() {
  usePageTitle('Factures')
  const navigate = useNavigate()
  const invoices = useResource(api.invoices)
  const customers = useResource(api.customers)

  async function createDraft(customerId: string) {
    const draft = await api.createDraft(customerId)
    await navigate(`/factures/${draft.id}`)
  }

  return (
    <>
      <h1>Factures</h1>
      <LoadState loading={customers.loading} error={customers.error} />
      {customers.data &&
        (customers.data.length > 0 ? (
          <NewDraftForm customers={customers.data} onCreate={createDraft} />
        ) : (
          <p>
            Pour créer une facture, <Link to="/clients/nouveau">ajoutez d'abord un client</Link>.
          </p>
        ))}

      <h2>Toutes les factures</h2>
      <LoadState loading={invoices.loading} error={invoices.error} />
      {invoices.data && invoices.data.length === 0 && <p>Aucune facture pour l'instant.</p>}
      {invoices.data && invoices.data.length > 0 && (
        <div className="table-scroll">
          <table>
            <caption className="visually-hidden">Factures, de la plus récente à la plus ancienne</caption>
            <thead>
              <tr>
                <th scope="col">Numéro</th>
                <th scope="col">Client</th>
                <th scope="col">État</th>
                <th scope="col" className="amount">
                  Total (CHF)
                </th>
                <th scope="col">Émise le</th>
                <th scope="col">Échéance</th>
                <th scope="col">Payée le</th>
              </tr>
            </thead>
            <tbody>
              {invoices.data.map((invoice) => (
                <tr key={invoice.id}>
                  <th scope="row">
                    <Link to={`/factures/${invoice.id}`}>{invoice.number ?? 'Brouillon'}</Link>
                  </th>
                  <td>{invoice.customerName}</td>
                  <td>
                    <StatusBadge status={invoice.status} overdue={invoice.overdue} />
                  </td>
                  <td className="amount">{formatChf(invoice.total)}</td>
                  <td>{formatDate(invoice.issueDate)}</td>
                  <td>{formatDate(invoice.dueDate)}</td>
                  <td>{formatDate(invoice.paidOn)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}
