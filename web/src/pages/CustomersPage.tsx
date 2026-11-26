import { Link, useLocation } from 'react-router'
import { api } from '../api'
import { LoadState } from '../components/LoadState'
import { usePageTitle } from '../components/usePageTitle'
import { useResource } from '../components/useResource'
import { streetLine, townLine } from '../format'

export function CustomersPage() {
  usePageTitle('Clients')
  const { data: customers, loading, error } = useResource(api.customers)
  const notice = (useLocation().state as { notice?: string } | null)?.notice

  return (
    <>
      <div className="page-head">
        <h1>Clients</h1>
        <Link className="button" to="/clients/nouveau">
          Nouveau client
        </Link>
      </div>
      <p role="status">{notice}</p>
      <LoadState loading={loading} error={error} />
      {customers && customers.length === 0 && <p>Aucun client pour l'instant.</p>}
      {customers && customers.length > 0 && (
        <div className="table-scroll">
          <table>
            <caption className="visually-hidden">Clients, par nom</caption>
            <thead>
              <tr>
                <th scope="col">Nom</th>
                <th scope="col">Adresse</th>
                <th scope="col">Localité</th>
                <th scope="col">Pays</th>
              </tr>
            </thead>
            <tbody>
              {customers.map((customer) => (
                <tr key={customer.id}>
                  <th scope="row">
                    <Link to={`/clients/${customer.id}`}>{customer.address.name}</Link>
                  </th>
                  <td>{streetLine(customer.address)}</td>
                  <td>{townLine(customer.address)}</td>
                  <td>{customer.address.country}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  )
}
