<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const authStore = useAuthStore()

const email = ref('')
const password = ref('')
const message = ref<string | null>(null)
const error = ref<string | null>(null)
const loading = ref(false)

async function submit() {
  loading.value = true
  error.value = null
  message.value = null

  try {
    const response = await authStore.register(email.value, password.value)
    message.value = response.message
    await router.push('/login')
  } catch (exception) {
    error.value = exception instanceof Error ? exception.message : 'Registration failed'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <section>
    <h1>Register</h1>

    <form @submit.prevent="submit">
      <label>
        Email
        <input v-model="email" type="email" required />
      </label>

      <label>
        Password
        <input v-model="password" type="password" required minlength="12" />
      </label>

      <button type="submit" :disabled="loading">
        {{ loading ? 'Creating account...' : 'Create account' }}
      </button>
    </form>

    <p v-if="message">{{ message }}</p>
    <p v-if="error">{{ error }}</p>
  </section>
</template>