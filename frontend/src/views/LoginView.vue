<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const authStore = useAuthStore()

const email = ref('')
const password = ref('')
const error = ref<string | null>(null)
const loading = ref(false)

async function submit() {
  loading.value = true
  error.value = null

  try {
    await authStore.login(email.value, password.value)
    await router.push('/app')
  } catch (exception) {
    error.value = exception instanceof Error ? exception.message : 'Login failed'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <section>
    <h1>Login</h1>

    <form @submit.prevent="submit">
      <label>
        Email
        <input v-model="email" type="email" required />
      </label>

      <label>
        Password
        <input v-model="password" type="password" required />
      </label>

      <button type="submit" :disabled="loading">
        {{ loading ? 'Signing in...' : 'Login' }}
      </button>
    </form>

    <p v-if="error">{{ error }}</p>
  </section>
</template>