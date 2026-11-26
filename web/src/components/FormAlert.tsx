import type { Ref } from 'react'

/**
 * The messages of a refused request that belong to no single field. role="alert" makes a screen
 * reader announce them as soon as they appear.
 */
export function FormAlert({ messages, alertRef }: { messages: readonly string[]; alertRef?: Ref<HTMLDivElement> }) {
  return (
    <div role="alert" ref={alertRef} tabIndex={-1} className={messages.length > 0 ? 'form-alert' : undefined}>
      {messages.length === 1 && <p>{messages[0]}</p>}
      {messages.length > 1 && (
        <ul>
          {messages.map((message) => (
            <li key={message}>{message}</li>
          ))}
        </ul>
      )}
    </div>
  )
}
