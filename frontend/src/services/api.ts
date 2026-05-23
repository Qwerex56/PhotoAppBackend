import axios from 'axios'

const authBaseUrl = import.meta.env.VITE_AUTH_API_BASE_URL ?? 'https://auth.localhost'
const mediaBaseUrl = import.meta.env.VITE_MEDIA_API_BASE_URL ?? 'https://media.localhost'

export const authApi = axios.create({
  baseURL: authBaseUrl,
})

export const mediaApi = axios.create({
  baseURL: mediaBaseUrl,
})

export function setAuthToken(token: string | null) {
  authApi.defaults.headers.common.Authorization = token ? `Bearer ${token}` : undefined
  mediaApi.defaults.headers.common.Authorization = token ? `Bearer ${token}` : undefined
}