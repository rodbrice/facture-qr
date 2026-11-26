import { useEffect, useRef } from 'react'
import { hasErrors, type FormErrors } from '../formErrors'

/**
 * After a refused submission, moves the focus to the first invalid field of the form, or to its
 * alert when no field is named, so that keyboard users land on the problem.
 */
export function useFocusOnErrors(errors: FormErrors) {
  const formRef = useRef<HTMLFormElement>(null)
  const alertRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!hasErrors(errors)) {
      return
    }

    const invalid = formRef.current?.querySelector<HTMLElement>('[aria-invalid="true"]')
    ;(invalid ?? alertRef.current)?.focus()
  }, [errors])

  return { formRef, alertRef }
}
