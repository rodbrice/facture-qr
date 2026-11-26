import { useCallback } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { api, type Address } from '../api'
import { LoadState } from '../components/LoadState'
import { usePageTitle } from '../components/usePageTitle'
import { useResource } from '../components/useResource'
import { CustomerForm } from '../forms/CustomerForm'

const emptyAddress: Address = { name: '', street: null, buildingNumber: null, postalCode: '', town: '', country: 'CH' }

/** Creates a customer (/clients/nouveau) or changes the address of one (/clients/:id). */
export function CustomerPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const isNew = id === undefined
  usePageTitle(isNew ? 'Nouveau client' : 'Modifier le client')
  const load = useCallback(() => (id === undefined ? Promise.resolve(null) : api.customer(id)), [id])
  const { data: customer, loading, error } = useResource(load)

  async function save(address: Address) {
    const saved = id === undefined ? await api.createCustomer(address) : await api.updateCustomer(id, address)
    await navigate('/clients', { state: { notice: `Client « ${saved.address.name} » enregistré.` } })
  }

  return (
    <>
      <p>
        <Link to="/clients">← Clients</Link>
      </p>
      <h1>{isNew ? 'Nouveau client' : 'Modifier le client'}</h1>
      {!isNew && <p>Les factures déjà créées gardent l'adresse qu'elles ont copiée.</p>}
      <LoadState loading={loading} error={error} />
      {(isNew || customer) && (
        <CustomerForm key={customer?.id ?? 'new'} initial={customer?.address ?? emptyAddress} submitLabel="Enregistrer" onSubmit={save} />
      )}
    </>
  )
}
