<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const authStore = useAuthStore()

const email = ref('')
const password = ref('')
const confirmPassword = ref('')
const message = ref<string | null>(null)
const error = ref<string | null>(null)
const loading = ref(false)

const passwordStrength = computed(() => {
  const pwd = password.value
  if (!pwd) return 0
  
  let strength = 0
  if (pwd.length >= 12) strength++
  if (pwd.length >= 16) strength++
  if (/[A-Z]/.test(pwd) && /[a-z]/.test(pwd)) strength++
  if (/\d/.test(pwd)) strength++
  if (/[^A-Za-z0-9]/.test(pwd)) strength++
  
  return strength
})

const passwordsMatch = computed(() => {
  return password.value === confirmPassword.value && password.value.length > 0
})

const isFormValid = computed(() => {
  return (
    email.value.includes('@') &&
    password.value.length >= 12 &&
    passwordsMatch.value
  )
})

async function submit() {
  if (!isFormValid.value) return
  
  loading.value = true
  error.value = null
  message.value = null

  try {
    const response = await authStore.register(email.value, password.value)
    message.value = response.message
    setTimeout(() => {
      router.push('/login')
    }, 2000)
  } catch (exception) {
    error.value = exception instanceof Error ? exception.message : 'Rejestracja nie powiodła się'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="register-container">
    <div class="register-card">
      <div class="register-header">
        <h1>PhotoApp</h1>
        <p class="subtitle">Utwórz nowe konto</p>
      </div>

      <form @submit.prevent="submit" class="register-form">
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
            minlength="12"
            required
            :disabled="loading"
          />
          
          <div class="password-requirements">
            <p class="requirement-title">Wymagania:</p>
            <ul>
              <li :class="{ met: password.length >= 12 }">
                <span class="checkmark">✓</span> Co najmniej 12 znaków
              </li>
              <li :class="{ met: /[A-Z]/.test(password) && /[a-z]/.test(password) }">
                <span class="checkmark">✓</span> Wielkie i małe litery
              </li>
              <li :class="{ met: /\d/.test(password) }">
                <span class="checkmark">✓</span> Co najmniej jedna cyfra
              </li>
              <li :class="{ met: /[^A-Za-z0-9]/.test(password) }">
                <span class="checkmark">✓</span> Co najmniej jeden znak specjalny
              </li>
            </ul>
          </div>

          <div class="strength-bar">
            <div :class="['strength-fill', `strength-${passwordStrength}`]"></div>
          </div>
        </div>

        <div class="form-group">
          <label for="confirm-password">Potwierdź hasło</label>
          <input
            id="confirm-password"
            v-model="confirmPassword"
            type="password"
            placeholder="••••••••"
            required
            :disabled="loading"
            :class="{ 'password-mismatch': confirmPassword && !passwordsMatch }"
          />
          <span v-if="confirmPassword && !passwordsMatch" class="error-text">
            Hasła się nie zgadzają
          </span>
        </div>

        <button
          type="submit"
          :disabled="loading || !isFormValid"
          class="btn btn-primary"
        >
          {{ loading ? 'Tworzenie konta...' : 'Utwórz konto' }}
        </button>
      </form>

      <div v-if="message" class="success-message">
        {{ message }}<br />
        <span style="font-size: 12px; opacity: 0.8;">Przekierowanie za 2 sekundy...</span>
      </div>

      <div v-if="error" class="error-message">
        {{ error }}
      </div>

      <div class="login-link">
        <p>Masz już konto? <router-link to="/login">Zaloguj się</router-link></p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.register-container {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  background: linear-gradient(135deg, #F5F1ED 0%, #E8E3D8 100%);
  padding: 20px;
}

.register-card {
  background: white;
  border-radius: 12px;
  box-shadow: var(--shadow);
  padding: 48px;
  max-width: 420px;
  width: 100%;
  border: 1px solid #E0D7CE;
}

.register-header {
  text-align: center;
  margin-bottom: 32px;
}

.register-header h1 {
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

.register-form {
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

.form-group input.password-mismatch {
  border-color: #C41E3A;
  background: rgba(196, 30, 58, 0.05);
}

.error-text {
  color: #C41E3A;
  font-size: 12px;
  margin-top: -4px;
}

.password-requirements {
  background: rgba(107, 99, 89, 0.05);
  padding: 12px;
  border-radius: 6px;
  margin-top: 8px;
}

.requirement-title {
  margin: 0 0 8px;
  font-weight: 500;
  color: #1D2620;
  font-size: 12px;
}

.password-requirements ul {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.password-requirements li {
  font-size: 12px;
  color: #6B6359;
  display: flex;
  align-items: center;
  gap: 6px;
  transition: color 0.2s ease;
}

.password-requirements li.met {
  color: #2D5016;
}

.checkmark {
  width: 16px;
  height: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 10px;
  color: transparent;
  transition: color 0.2s ease;
}

.password-requirements li.met .checkmark {
  color: #2D5016;
}

.strength-bar {
  height: 4px;
  background: #E0D7CE;
  border-radius: 2px;
  overflow: hidden;
}

.strength-fill {
  height: 100%;
  width: 0%;
  transition: width 0.3s ease, background-color 0.3s ease;
  border-radius: 2px;
}

.strength-fill.strength-0 {
  width: 0%;
}

.strength-fill.strength-1 {
  width: 25%;
  background: #C41E3A;
}

.strength-fill.strength-2 {
  width: 50%;
  background: #F59E0B;
}

.strength-fill.strength-3 {
  width: 75%;
  background: #10B981;
}

.strength-fill.strength-4,
.strength-fill.strength-5 {
  width: 100%;
  background: #059669;
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

.success-message {
  background: #E6F9F0;
  color: #2D5016;
  padding: 12px 16px;
  border-radius: 8px;
  border-left: 4px solid #2D5016;
  font-size: 14px;
  margin-top: 16px;
}

.error-message {
  background: #FFE8E8;
  color: #C41E3A;
  padding: 12px 16px;
  border-radius: 8px;
  border-left: 4px solid #C41E3A;
  font-size: 14px;
  margin-top: 16px;
}

.login-link {
  text-align: center;
  font-size: 14px;
  color: #6B6359;
  margin-top: 24px;
}

.login-link a {
  color: #6B6359;
  text-decoration: none;
  font-weight: 600;
  transition: color 0.2s ease;
}

.login-link a:hover {
  color: #1D2620;
}

@media (max-width: 640px) {
  .register-card {
    padding: 32px 24px;
  }

  .register-header h1 {
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