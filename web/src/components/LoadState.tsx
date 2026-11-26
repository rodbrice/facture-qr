import { ApiError } from '../api'

/** What a page shows while its data loads, or when it could not be loaded. */
export function LoadState({ loading, error }: { loading: boolean; error?: unknown }) {
  if (error) {
    const message = error instanceof ApiError ? error.message : String(error)
    return (
      <p role="alert" className="form-alert">
        Chargement impossible : {message}
      </p>
    )
  }

  return loading ? <p role="status">Chargement…</p> : null
}
