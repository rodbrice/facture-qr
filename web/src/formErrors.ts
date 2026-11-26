import { ApiError } from './api'

/** The messages of a refused request, split between the fields of a form and the form as a whole. */
export interface FormErrors {
  fields: Record<string, string>
  form: string[]
}

export const noErrors: FormErrors = { fields: {}, form: [] }

export function hasErrors(errors: FormErrors): boolean {
  return errors.form.length > 0 || Object.keys(errors.fields).length > 0
}

/**
 * Places the errors of an API answer: a 400 error keyed by one of `fields` goes next to that field,
 * every other message (another key, a 404, a 409, no answer) goes to the form.
 */
export function formErrors(error: unknown, fields: readonly string[]): FormErrors {
  if (!(error instanceof ApiError)) {
    return { fields: {}, form: [`Erreur inattendue : ${error instanceof Error ? error.message : String(error)}`] }
  }

  const result: FormErrors = { fields: {}, form: [] }
  for (const [field, messages] of Object.entries(error.fieldErrors)) {
    const text = messages.join(' ')
    if (fields.includes(field)) {
      result.fields[field] = text
    } else {
      result.form.push(text)
    }
  }

  if (!hasErrors(result)) {
    result.form.push(error.message)
  }

  return result
}
