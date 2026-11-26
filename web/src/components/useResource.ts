import { useCallback, useEffect, useState } from 'react'

interface Answer<T> {
  load?: () => Promise<T>
  data?: T
  error?: unknown
}

/**
 * Loads data from the API whenever `load` changes (wrap it in useCallback). While the answer for
 * the current `load` has not arrived, `loading` is true. setData replaces the data with a newer answer.
 */
export function useResource<T>(load: () => Promise<T>) {
  const [answer, setAnswer] = useState<Answer<T>>({})

  useEffect(() => {
    let current = true
    load().then(
      (data) => current && setAnswer({ load, data }),
      (error: unknown) => current && setAnswer({ load, error }),
    )
    return () => {
      current = false
    }
  }, [load])

  const setData = useCallback((data: T) => setAnswer({ load, data }), [load])
  const loading = answer.load !== load

  return { data: loading ? undefined : answer.data, error: loading ? undefined : answer.error, loading, setData }
}
