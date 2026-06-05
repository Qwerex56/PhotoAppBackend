import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { authApi, setAuthToken } from '../services/api'

type LoginResponse = {
  userId: string
  email: string
  accessToken: string
  refreshToken: string
  accessTokenExpiresIn: number
  requiresMfa: boolean
  mfaChallenge: string | null
}

type RegisterResponse = {
  userId: string
  email: string
  message: string
}

type OAuthStartResponse = {
  authorizationUrl: string
}

const storageKey = 'photoapp.auth'

export const useAuthStore = defineStore('auth', () => {
  const userId = ref<string | null>(null)
  const email = ref<string | null>(null)
  const accessToken = ref<string | null>(null)
  const refreshToken = ref<string | null>(null)
  const initialized = ref(false)
  const status = ref<string | null>(null)
  const pendingMfaChallenge = ref<string | null>(null)

  const isAuthenticated = computed(() => Boolean(accessToken.value))

  function persistState() {
    const payload = {
      userId: userId.value,
      email: email.value,
      accessToken: accessToken.value,
      refreshToken: refreshToken.value,
    }

    localStorage.setItem(storageKey, JSON.stringify(payload))
  }

  function restoreState() {
    const raw = localStorage.getItem(storageKey)
    if (!raw) {
      initialized.value = true
      return
    }

    try {
      const payload = JSON.parse(raw) as {
        userId: string | null
        email: string | null
        accessToken: string | null
        refreshToken: string | null
      }

      userId.value = payload.userId
      email.value = payload.email
      accessToken.value = payload.accessToken
      refreshToken.value = payload.refreshToken
      setAuthToken(accessToken.value)
    } finally {
      initialized.value = true
    }
  }

  async function register(emailValue: string, password: string) {
    status.value = null

    const response = await authApi.post<RegisterResponse>('/api/auth/register', {
      Email: emailValue,
      Password: password,
    })

    status.value = response.data.message
    return response.data
  }

  async function login(emailValue: string, password: string) {
    status.value = null

    const response = await authApi.post<LoginResponse>('/api/auth/login', {
      Email: emailValue,
      Password: password,
    })

    if (response.data.requiresMfa) {
      pendingMfaChallenge.value = response.data.mfaChallenge
      status.value = 'Wysłano kod potwierdzający na e-mail.'
      return response.data
    }

    userId.value = response.data.userId
    email.value = response.data.email
    accessToken.value = response.data.accessToken
    refreshToken.value = response.data.refreshToken
    setAuthToken(accessToken.value)
    persistState()

    return response.data
  }

  async function completeEmailMfa(code: string) {
    if (!pendingMfaChallenge.value) {
      throw new Error('Brak aktywnego potwierdzenia logowania')
    }

    const response = await authApi.post<LoginResponse>('/api/auth/mfa/email/verify', {
      ChallengeToken: pendingMfaChallenge.value,
      Code: code,
    })

    userId.value = response.data.userId
    email.value = response.data.email
    accessToken.value = response.data.accessToken
    refreshToken.value = response.data.refreshToken
    setAuthToken(accessToken.value)
    pendingMfaChallenge.value = null
    persistState()

    return response.data
  }

  async function beginGoogleOAuth() {
    const response = await authApi.get<OAuthStartResponse>('/api/auth/oauth/google/start')
    window.location.assign(response.data.authorizationUrl)
  }

  async function completeGoogleOAuth(code: string, state: string) {
    const response = await authApi.post<LoginResponse>('/api/auth/oauth/google/callback', {
      Code: code,
      State: state,
    })

    userId.value = response.data.userId
    email.value = response.data.email
    accessToken.value = response.data.accessToken
    refreshToken.value = response.data.refreshToken
    setAuthToken(accessToken.value)
    persistState()

    return response.data
  }

  function logout() {
    userId.value = null
    email.value = null
    accessToken.value = null
    refreshToken.value = null
    status.value = null
    pendingMfaChallenge.value = null
    setAuthToken(null)
    localStorage.removeItem(storageKey)
  }

  function initialize() {
    if (initialized.value) {
      return
    }

    restoreState()
  }

  return {
    userId,
    email,
    accessToken,
    refreshToken,
    initialized,
    status,
    pendingMfaChallenge,
    isAuthenticated,
    initialize,
    login,
    completeEmailMfa,
    beginGoogleOAuth,
    completeGoogleOAuth,
    logout,
    register,
  }
})