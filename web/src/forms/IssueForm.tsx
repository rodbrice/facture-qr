import { useState, type FormEvent } from 'react'
import type { IssueDates } from '../api'
import { FormAlert } from '../components/FormAlert'
import { TextField } from '../components/TextField'
import { useFocusOnErrors } from '../components/useFocusOnErrors'
import { formErrors, noErrors, type FormErrors } from '../formErrors'

const fields = ['issueDate', 'dueDate']

/** Issues a draft. An empty date is left out of the request, so the API uses its default. */
export function IssueForm({ onIssue }: { onIssue: (dates: IssueDates) => Promise<void> }) {
  const [issueDate, setIssueDate] = useState('')
  const [dueDate, setDueDate] = useState('')
  const [errors, setErrors] = useState<FormErrors>(noErrors)
  const [busy, setBusy] = useState(false)
  const { formRef, alertRef } = useFocusOnErrors(errors)

  async function submit(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    try {
      await onIssue({ issueDate: issueDate || undefined, dueDate: dueDate || undefined })
      setErrors(noErrors)
    } catch (error) {
      setErrors(formErrors(error, fields))
    } finally {
      setBusy(false)
    }
  }

  return (
    <form ref={formRef} onSubmit={submit} noValidate aria-labelledby="issue-title" className="panel">
      <h3 id="issue-title">Émettre la facture</h3>
      <p>L'émission attribue le numéro suivant de l'année ; la facture ne peut ensuite plus être modifiée.</p>
      <div className="field-row">
        <TextField id="issue-date" type="date" label="Date d'émission" hint="Vide : aujourd'hui." value={issueDate} onChange={setIssueDate} error={errors.fields.issueDate} />
        <TextField id="due-date" type="date" label="Échéance" hint="Vide : 30 jours après l'émission." value={dueDate} onChange={setDueDate} error={errors.fields.dueDate} />
      </div>
      <FormAlert messages={errors.form} alertRef={alertRef} />
      <button type="submit" disabled={busy}>
        Émettre
      </button>
    </form>
  )
}
