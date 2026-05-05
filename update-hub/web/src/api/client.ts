const API_BASE = '/api'

export interface ApiError {
  error: string
}

export async function apiFetch<T>(
  path: string,
  options: RequestInit = {},
): Promise<T> {
  const url = `${API_BASE}${path}`
  const res = await fetch(url, {
    ...options,
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
  })

  if (res.status === 401) {
    throw new Error('unauthenticated')
  }

  if (!res.ok) {
    let message = `request failed: ${res.status}`
    try {
      const body = (await res.json()) as ApiError
      if (body.error) message = body.error
    } catch {
      // use default message
    }
    throw new Error(message)
  }

  return res.json() as Promise<T>
}
