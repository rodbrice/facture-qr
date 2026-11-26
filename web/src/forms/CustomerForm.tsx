import { useState, type FormEvent } from 'react'
import type { Address } from '../api'
import { FormAlert } from '../components/FormAlert'
import { TextField } from '../components/TextField'
import { useFocusOnErrors } from '../components/useFocusOnErrors'
import { formErrors, noErrors, type FormErrors } from '../formErrors'

const fields = ['name', 'street', 'buildingNumber', 'postalCode', 'town', 'country'] as const
type Field = (typeof fields)[number]
type Values = Record<Field, string>

interface CustomerFormProps {
  initial: Address
  submitLabel: string
  onSubmit: (address: Address) => Promise<void>
}

/**
 * The address of a customer. The rules (lengths, characters, country code) are checked by the API;
 * the form shows its answer next to the field it names.
 */
export function CustomerForm({ initial, submitLabel, onSubmit }: CustomerFormProps) {
  const [values, setValues] = useState<Values>(() => ({
    name: initial.name,
    street: initial.street ?? '',
    buildingNumber: initial.buildingNumber ?? '',
    postalCode: initial.postalCode,
    town: initial.town,
    country: initial.country,
  }))
  const [errors, setErrors] = useState<FormErrors>(noErrors)
  const [busy, setBusy] = useState(false)
  const { formRef, alertRef } = useFocusOnErrors(errors)

  const set = (field: Field) => (value: string) => setValues((previous) => ({ ...previous, [field]: value }))

  async function submit(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    try {
      await onSubmit({
        name: values.name.trim(),
        street: values.street.trim() || null,
        buildingNumber: values.buildingNumber.trim() || null,
        postalCode: values.postalCode.trim(),
        town: values.town.trim(),
        country: values.country.trim().toUpperCase(),
      })
      setErrors(noErrors)
    } catch (error) {
      setErrors(formErrors(error, fields))
    } finally {
      setBusy(false)
    }
  }

  const field = (name: Field) => ({ id: `customer-${name}`, value: values[name], onChange: set(name), error: errors.fields[name] })

  return (
    <form ref={formRef} onSubmit={submit} noValidate aria-label="Adresse du client">
      <TextField {...field('name')} label="Nom ou raison sociale" required autoComplete="name" />
      <div className="field-row">
        <TextField {...field('street')} label="Rue ou case postale" autoComplete="address-line1" />
        <TextField {...field('buildingNumber')} label="Numéro" />
      </div>
      <div className="field-row">
        <TextField {...field('postalCode')} label="NPA" required autoComplete="postal-code" />
        <TextField {...field('town')} label="Localité" required autoComplete="address-level2" />
      </div>
      <TextField {...field('country')} label="Pays" hint="Code ISO à deux lettres, par exemple CH." required autoComplete="country" />
      <FormAlert messages={errors.form} alertRef={alertRef} />
      <button type="submit" disabled={busy}>
        {submitLabel}
      </button>
    </form>
  )
}
