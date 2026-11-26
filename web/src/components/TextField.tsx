import type { Ref } from 'react'

interface TextFieldProps {
  id: string
  label: string
  value: string
  onChange: (value: string) => void
  error?: string
  hint?: string
  required?: boolean
  type?: 'text' | 'date'
  inputMode?: 'text' | 'decimal'
  autoComplete?: string
  inputRef?: Ref<HTMLInputElement>
}

/**
 * A labelled input. Its hint and its error are linked with aria-describedby, and an error
 * sets aria-invalid, so a screen reader reads them with the field.
 */
export function TextField({ id, label, value, onChange, error, hint, required, type = 'text', inputMode, autoComplete, inputRef }: TextFieldProps) {
  const hintId = `${id}-hint`
  const errorId = `${id}-error`
  const describedBy = [hint ? hintId : null, error ? errorId : null].filter(Boolean).join(' ')

  return (
    <div className="field">
      <label htmlFor={id}>
        {label}
        {required && <span aria-hidden="true"> *</span>}
      </label>
      {hint && (
        <p id={hintId} className="hint">
          {hint}
        </p>
      )}
      <input
        id={id}
        ref={inputRef}
        type={type}
        inputMode={inputMode}
        autoComplete={autoComplete}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        aria-required={required || undefined}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy || undefined}
      />
      {error && (
        <p id={errorId} className="field-error">
          {error}
        </p>
      )}
    </div>
  )
}
