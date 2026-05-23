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

const storageKey = 'photoapp.demo.auth'

export const useAuthStore = defineStore('auth', () => {
  const userId = ref<string | null>(null)
  const email = ref<string | null>(null)
  const accessToken = ref<string | null>(null)
  const refreshToken = ref<string | null>(null)
  const initialized = ref(false)
  const status = ref<string | null>(null)

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
      email: emailValue,
      password,
    })

    status.value = response.data.message
    return response.data
  }

  async function login(emailValue: string, password: string) {
    status.value = null

    const response = await authApi.post<LoginResponse>('/api/auth/login', {
      email: emailValue,
      password,
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
    isAuthenticated,
    initialize,
    login,
    logout,
    register,
  }
})