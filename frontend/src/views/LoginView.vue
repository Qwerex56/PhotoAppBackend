<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const authStore = useAuthStore()

const email = ref('')
const password = ref('')
const mfaCode = ref('')
const error = ref<string | null>(null)
const loading = ref(false)
const mfaPending = ref(false)
const resendLoading = ref(false)

const isFormValid = computed(() => {
  if (mfaPending.value) {
    return mfaCode.value.length === 6
  }
  return email.value && password.value && email.value.includes('@')
})

async function submit() {
  if (!isFormValid.value) return
  
  loading.value = true
  error.value = null

  try {
    const response = await authStore.login(email.value, password.value)

    if (response.requiresMfa) {
      mfaPending.value = true
      return
    }

    await router.push('/app')
  } catch (exception) {
    error.value = exception instanceof Error ? exception.message : 'Logowanie nie powiodło się'
  } finally {
    loading.value = false
  }
}

async function verifyMfa() {
  if (mfaCode.value.length !== 6) {
    error.value = 'Kod musi zawierać 6 cyfr'
    return
  }

  loading.value = true
  error.value = null

  try {
    await authStore.completeEmailMfa(mfaCode.value)
    await router.push('/app')
  } catch (exception) {
    error.value = exception instanceof Error ? exception.message : 'Weryfikacja 2FA nie powiodła się'
  } finally {
    loading.value = false
  }
}

async function resendMfaCode() {
  resendLoading.value = true
  error.value = null

  try {
    await authStore.login(email.value, password.value)
    error.value = null
  } catch (exception) {
    error.value = 'Nie udało się wysłać kodu'
  } finally {
    resendLoading.value = false
  }
}

async function signInWithGoogle() {
  loading.value = true
  error.value = null

  try {
    await authStore.beginGoogleOAuth()
  } catch (exception) {
    error.value = exception instanceof Error ? exception.message : 'Logowanie Google nie powiodło się'
    loading.value = false
  }
}

function backToLogin() {
  mfaPending.value = false
  email.value = ''
  password.value = ''
  mfaCode.value = ''
  error.value = null
}
</script>

<template>
  <div class="login-container">
    <div class="login-card">
      <div class="login-header">
        <h1>PhotoApp</h1>
        <p v-if="!mfaPending" class="subtitle">Zaloguj się do konta</p>
        <p v-else class="subtitle">Potwierdź kod weryfikacyjny</p>
      </div>

      <!-- Login Form -->
      <form v-if="!mfaPending" @submit.prevent="submit" class="login-form">
        <div class="form-group">
          <label for="email">Email</label>
          <input
            id="email"
            v-model="email"
            type="email"
            placeholder="twoj@email.com"
            required
            :disabled="loading"
          />
        </div>

        <div class="form-group">
          <label for="password">Hasło</label>
          <input
            id="password"
            v-model="password"
            type="password"
            placeholder="••••••••"
            required
            :disabled="loading"
          />
        </div>

        <button
          type="submit"
          :disabled="loading || !isFormValid"
          class="btn btn-primary"
        >
          {{ loading ? 'Zalogowanie...' : 'Zaloguj się' }}
        </button>
      </form>

      <!-- MFA Verification -->
      <div v-else class="mfa-section">
        <p class="mfa-message">
          Wysłaliśmy kod weryfikacyjny na Twój adres email.<br />
          <strong>{{ email }}</strong>
        </p>

        <div class="form-group">
          <label for="mfa-code">Kod weryfikacyjny (6 cyfr)</label>
          <input
            id="mfa-code"
            v-model="mfaCode"
            type="text"
            inputmode="numeric"
            maxlength="6"
            placeholder="000000"
            :disabled="loading"
            class="mfa-input"
          />
        </div>

        <button
          type="button"
          :disabled="loading || !isFormValid"
          @click="verifyMfa"
          class="btn btn-primary"
        >
          {{ loading ? 'Weryfikacja...' : 'Potwierdź kod' }}
        </button>

        <button
          type="button"
          :disabled="resendLoading"
          @click="resendMfaCode"
          class="btn btn-secondary"
        >
          {{ resendLoading ? 'Wysyłanie...' : 'Wyślij kod ponownie' }}
        </button>

        <button
          type="button"
          @click="backToLogin"
          class="btn btn-text"
        >
          Wróć do logowania
        </button>
      </div>

      <!-- Error Message -->
      <div v-if="error" class="error-message">
        {{ error }}
      </div>

      <!-- Google OAuth -->
      <div v-if="!mfaPending" class="divider">
        <span>lub</span>
      </div>

      <button
        v-if="!mfaPending"
        type="button"
        :disabled="loading"
        @click="signInWithGoogle"
        class="btn btn-google"
      >
        <svg class="google-icon" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
          <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4"/>
          <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"/>
          <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" fill="#FBBC05"/>
          <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335"/>
        </svg>
        Zaloguj się Google
      </button>

      <!-- Register Link -->
      <div v-if="!mfaPending" class="register-link">
        <p>Nie masz konta? <router-link to="/register">Zarejestruj się</router-link></p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.login-container {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  background: linear-gradient(135deg, #F5F1ED 0%, #E8E3D8 100%);
  padding: 20px;
}

.login-card {
  background: white;
  border-radius: 12px;
  box-shadow: var(--shadow);
  padding: 48px;
  max-width: 420px;
  width: 100%;
  border: 1px solid #E0D7CE;
}

.login-header {
  text-align: center;
  margin-bottom: 32px;
}

.login-header h1 {
  margin: 0 0 8px;
  font-size: 32px;
  color: #1D2620;
  font-weight: 600;
}

.subtitle {
  color: #6B6359;
  font-size: 16px;
  margin: 0;
}

.login-form,
.mfa-section {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.form-group label {
  font-weight: 500;
  color: #1D2620;
  font-size: 14px;
}

.form-group input {
  padding: 12px 16px;
  border: 1px solid #C4BAA8;
  border-radius: 8px;
  font-size: 16px;
  font-family: var(--sans);
  background: #F5F1ED;
  color: #1D2620;
  transition: all 0.2s ease;
}

.form-group input:focus {
  outline: none;
  border-color: #6B6359;
  background: white;
  box-shadow: 0 0 0 3px rgba(107, 99, 89, 0.1);
}

.form-group input:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.mfa-input {
  font-size: 24px !important;
  letter-spacing: 8px;
  text-align: center;
  font-weight: 600;
  font-family: var(--mono);
}

.mfa-message {
  background: rgba(107, 99, 89, 0.05);
  padding: 16px;
  border-radius: 8px;
  border-left: 4px solid #6B6359;
  color: #1D2620;
  font-size: 14px;
  line-height: 1.6;
}

.btn {
  padding: 12px 16px;
  border: none;
  border-radius: 8px;
  font-size: 16px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
  font-family: var(--sans);
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-primary {
  background: #6B6359;
  color: white;
  width: 100%;
}

.btn-primary:hover:not(:disabled) {
  background: #584B42;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(107, 99, 89, 0.2);
}

.btn-secondary {
  background: #E8E3D8;
  color: #1D2620;
  width: 100%;
}

.btn-secondary:hover:not(:disabled) {
  background: #D5CCC1;
}

.btn-text {
  background: transparent;
  color: #6B6359;
  width: 100%;
  border-bottom: 1px solid transparent;
}

.btn-text:hover:not(:disabled) {
  border-bottom-color: #6B6359;
}

.btn-google {
  background: white;
  color: #1D2620;
  border: 1px solid #C4BAA8;
  width: 100%;
}

.btn-google:hover:not(:disabled) {
  background: #F5F1ED;
  border-color: #6B6359;
}

.google-icon {
  width: 20px;
  height: 20px;
}

.error-message {
  background: #FFE8E8;
  color: #C41E3A;
  padding: 12px 16px;
  border-radius: 8px;
  border-left: 4px solid #C41E3A;
  font-size: 14px;
}

.divider {
  display: flex;
  align-items: center;
  gap: 16px;
  color: #6B6359;
  font-size: 14px;
  margin: 16px 0;
}

.divider::before,
.divider::after {
  content: '';
  flex: 1;
  height: 1px;
  background: #C4BAA8;
}

.register-link {
  text-align: center;
  font-size: 14px;
  color: #6B6359;
}

.register-link a {
  color: #6B6359;
  text-decoration: none;
  font-weight: 600;
  transition: color 0.2s ease;
}

.register-link a:hover {
  color: #1D2620;
}

@media (max-width: 640px) {
  .login-card {
    padding: 32px 24px;
  }

  .login-header h1 {
    font-size: 28px;
  }

  .form-group input {
    font-size: 16px;
  }

  .btn {
    padding: 11px 14px;
    font-size: 15px;
  }
}
</style>