// composables/useAuth.ts
export const useAuth = () => {
  const token = useCookie('auth_token', {
    maxAge: 60 * 60 * 24,  // 24 Stunden (oder was dein JWT TTL ist)
    secure: true,  // wenn in Produktion, dann true, sonst false
    sameSite: 'strict'
  })

  const user = useState('user', () => null)

  const login = async (credentials: { email: string, password: string }) => {
    try {
      // Request an dein .NET Backend
      const response = await $fetch<{ token: string }>('https://your-dotnet-api.com/api/auth/login', {
        method: 'POST',
        body: credentials
      })

      // Token speichern
      token.value = response.token

      // Optional: User-Info aus JWT extrahieren (siehe unten)
      user.value = parseJWT(response.token)

      return { success: true }
    } catch (error) {
      return { success: false, error }
    }
  }

  const logout = () => {
    token.value = null
    user.value = null
  }

  const isAuthenticated = computed(() => !!token.value)

  return {
    token,
    user,
    login,
    logout,
    isAuthenticated
  }
}

// Hilfsfunktion um User-Info aus JWT zu extrahieren
function parseJWT(token: string) {
  try {
    const base64Url = token.split('.')[1]
    if (!base64Url) return null
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/')
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    )
    return JSON.parse(jsonPayload)
  } catch {
    return null
  }
}
