import { api } from '../api'
import { LoadState } from '../components/LoadState'
import { usePageTitle } from '../components/usePageTitle'
import { useResource } from '../components/useResource'
import { streetLine, townLine } from '../format'

/** The creditor printed on every invoice. It is read from the API's configuration and cannot be changed here. */
export function SettingsPage() {
  usePageTitle('Paramètres')
  const { data: creditor, loading, error } = useResource(api.creditor)

  return (
    <>
      <h1>Paramètres</h1>
      <p>
        Le créancier figure sur chaque facture et sur sa section paiement. Il est lu dans la configuration de l'API (section{' '}
        <code>Invoicing:Creditor</code>) et vérifié au démarrage ; pour le changer, modifiez la configuration puis redémarrez l'API.
      </p>
      <LoadState loading={loading} error={error} />
      {creditor && (
        <dl className="settings">
          <dt>Nom</dt>
          <dd>{creditor.address.name}</dd>
          <dt>Adresse</dt>
          <dd>{streetLine(creditor.address) || '—'}</dd>
          <dt>Localité</dt>
          <dd>{townLine(creditor.address)}</dd>
          <dt>Pays</dt>
          <dd>{creditor.address.country}</dd>
          <dt>Compte</dt>
          <dd>{creditor.iban}</dd>
          <dt>Type de compte</dt>
          <dd>{creditor.isQrIban ? 'QR-IBAN : les factures portent une référence QR' : 'IBAN : les factures portent une référence de créancier (SCOR)'}</dd>
        </dl>
      )}
    </>
  )
}
