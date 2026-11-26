import { useRef, useState, type FormEvent } from 'react'
import type { NewLine } from '../api'
import { FormAlert } from '../components/FormAlert'
import { TextField } from '../components/TextField'
import { useFocusOnErrors } from '../components/useFocusOnErrors'
import { parseDecimal } from '../format'
import { formErrors, hasErrors, noErrors, type FormErrors } from '../formErrors'

const notANumber = 'Saisissez un nombre, par exemple 7.5 ou 7,5.'

/**
 * A new line of a draft. The form only turns the typed numbers into numbers; the API checks the
 * line (quantity, decimals, printable description) and computes its amount and the total.
 */
export function LineForm({ onAdd }: { onAdd: (line: NewLine) => Promise<void> }) {
  const [description, setDescription] = useState('')
  const [quantity, setQuantity] = useState('1')
  const [unitPrice, setUnitPrice] = useState('')
  const [errors, setErrors] = useState<FormErrors>(noErrors)
  const [busy, setBusy] = useState(false)
  const { formRef, alertRef } = useFocusOnErrors(errors)
  const descriptionRef = useRef<HTMLInputElement>(null)

  async function submit(event: FormEvent) {
    event.preventDefault()
    const parsedQuantity = parseDecimal(quantity)
    const parsedPrice = parseDecimal(unitPrice)
    const local: FormErrors = { fields: {}, form: [] }
    if (parsedQuantity === null) {
      local.fields.quantity = notANumber
    }
    if (parsedPrice === null) {
      local.fields.unitPrice = notANumber
    }
    if (hasErrors(local) || parsedQuantity === null || parsedPrice === null) {
      setErrors(local)
      return
    }

    setBusy(true)
    try {
      await onAdd({ description, quantity: parsedQuantity, unitPrice: parsedPrice })
      setErrors(noErrors)
      setDescription('')
      setQuantity('1')
      setUnitPrice('')
      descriptionRef.current?.focus()
    } catch (error) {
      // The API names the whole line ("line"): its message says which value is wrong.
      setErrors(formErrors(error, []))
    } finally {
      setBusy(false)
    }
  }

  return (
    <form ref={formRef} onSubmit={submit} noValidate aria-labelledby="new-line-title" className="panel">
      <h3 id="new-line-title">Ajouter une ligne</h3>
      <TextField id="line-description" label="Description" value={description} onChange={setDescription} inputRef={descriptionRef} required />
      <div className="field-row">
        <TextField id="line-quantity" label="Quantité" value={quantity} onChange={setQuantity} inputMode="decimal" error={errors.fields.quantity} required />
        <TextField id="line-unit-price" label="Prix unitaire (CHF)" value={unitPrice} onChange={setUnitPrice} inputMode="decimal" error={errors.fields.unitPrice} required />
      </div>
      <FormAlert messages={errors.form} alertRef={alertRef} />
      <button type="submit" disabled={busy}>
        Ajouter la ligne
      </button>
    </form>
  )
}
