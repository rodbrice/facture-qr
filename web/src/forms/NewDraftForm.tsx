import { useState, type FormEvent } from 'react'
import type { Customer } from '../api'
import { FormAlert } from '../components/FormAlert'
import { useFocusOnErrors } from '../components/useFocusOnErrors'
import { formErrors, noErrors, type FormErrors } from '../formErrors'
import { townLine } from '../format'

/** Creates an empty draft for a customer; its lines are added in the editor. */
export function NewDraftForm({ customers, onCreate }: { customers: readonly Customer[]; onCreate: (customerId: string) => Promise<void> }) {
  const [customerId, setCustomerId] = useState('')
  const [errors, setErrors] = useState<FormErrors>(noErrors)
  const [busy, setBusy] = useState(false)
  const { formRef, alertRef } = useFocusOnErrors(errors)
  const error = errors.fields.customerId

  async function submit(event: FormEvent) {
    event.preventDefault()
    if (!customerId) {
      setErrors({ fields: { customerId: 'Choisissez un client.' }, form: [] })
      return
    }

    setBusy(true)
    try {
      await onCreate(customerId)
    } catch (e) {
      setErrors(formErrors(e, ['customerId']))
      setBusy(false)
    }
  }

  return (
    <form ref={formRef} onSubmit={submit} noValidate aria-labelledby="new-draft-title" className="panel">
      <h2 id="new-draft-title">Nouvelle facture</h2>
      <div className="field">
        <label htmlFor="draft-customer">Client</label>
        <select
          id="draft-customer"
          value={customerId}
          onChange={(event) => setCustomerId(event.target.value)}
          aria-invalid={error ? true : undefined}
          aria-describedby={error ? 'draft-customer-error' : undefined}
        >
          <option value="">Choisir un client…</option>
          {customers.map((customer) => (
            <option key={customer.id} value={customer.id}>
              {customer.address.name}, {townLine(customer.address)}
            </option>
          ))}
        </select>
        {error && (
          <p id="draft-customer-error" className="field-error">
            {error}
          </p>
        )}
      </div>
      <FormAlert messages={errors.form} alertRef={alertRef} />
      <button type="submit" disabled={busy}>
        Créer un brouillon
      </button>
    </form>
  )
}
