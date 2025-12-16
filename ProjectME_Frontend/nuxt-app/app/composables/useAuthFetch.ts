// composables/useAuthFetch.ts
import type { UseFetchOptions } from 'nuxt/app'

export function useAuthFetch<T>(
  url: string | (() => string),
  options: UseFetchOptions<T> = {}
) {
  const token = useCookie('auth_token')

  // Custom $fetch mit Interceptors
  const customFetch = $fetch.create({
    baseURL: '/api', // deine API Base URL

    // Request Interceptor - Token automatisch hinzufügen
    onRequest({ options }) {
      if (token.value) {
      const headers = new Headers(options.headers as HeadersInit)
      headers.set('Authorization', `Bearer ${token.value}`)
      options.headers = headers
      }
    },

    // Response Interceptor - Fehlerbehandlung
    onResponseError({ response }) {
      if (response.status === 401) {
        // Token abgelaufen - automatisch ausloggen
        token.value = null
        navigateTo('/login')
      }
    }
  })

  // useFetch mit custom $fetch verwenden
  return useFetch(url, {
    ...options,
    $fetch: customFetch
  })
}
