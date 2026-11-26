import { useState, type FormEvent } from 'react'
import { FormAlert } from '../components/FormAlert'
import { TextField } from '../components/TextField'
import { useFocusOnErrors } from '../components/useFocusOnErrors'
import { formErrors, noErrors, type FormErrors } from '../formErrors'

/** Records the payment of an issued invoice. An empty date is left out, so the API uses today. */
export function MarkPaidForm({ onMarkPaid }: { onMarkPaid: (paidOn?: string) => Promise<void> }) {
  const [paidOn, setPaidOn] = useState('')
  const [errors, setErrors] = useState<FormErrors>(noErrors)
  const [busy, setBusy] = useState(false)
  const { formRef, alertRef } = useFocusOnErrors(errors)

  async function submit(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    try {
      await onMarkPaid(paidOn || undefined)
      setErrors(noErrors)
    } catch (error) {
      setErrors(formErrors(error, ['paidOn']))
    } finally {
      setBusy(false)
    }
  }

  return (
    <form ref={formRef} onSubmit={submit} noValidate aria-labelledby="paid-title" className="panel">
      <h3 id="paid-title">Enregistrer le paiement</h3>
      <TextField id="paid-on" type="date" label="Date du paiement" hint="Vide : aujourd'hui." value={paidOn} onChange={setPaidOn} error={errors.fields.paidOn} />
      <FormAlert messages={errors.form} alertRef={alertRef} />
      <button type="submit" disabled={busy}>
        Marquer comme payée
      </button>
    </form>
  )
}
