<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const status = ref('Finalizowanie logowania Google...')
const error = ref<string | null>(null)

onMounted(async () => {
  const code = typeof route.query.code === 'string' ? route.query.code : ''
  const state = typeof route.query.state === 'string' ? route.query.state : ''

  if (!code || !state) {
    error.value = 'Brakujące parametry logowania OAuth.'
    status.value = ''
    return
  }

  try {
    await authStore.completeGoogleOAuth(code, state)
    await router.push('/app')
  } catch (exception) {
    error.value = exception instanceof Error ? exception.message : 'Logowanie Google nie powiodło się'
    status.value = ''
  }
})
</script>

<template>
  <div class="callback-container">
    <div class="callback-card">
      <div class="loading-spinner"></div>
      <h1>{{ status || 'Przetwarzanie...' }}</h1>
      <p v-if="status" class="status-message">Czekaj, Cię przekierowujemy...</p>
      <p v-if="error" class="error-message">{{ error }}</p>
    </div>
  </div>
</template>

<style scoped>
.callback-container {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  background: linear-gradient(135deg, #F5F1ED 0%, #E8E3D8 100%);
}

.callback-card {
  background: white;
  border-radius: 12px;
  padding: 48px;
  max-width: 420px;
  width: 100%;
  text-align: center;
  box-shadow: 0 10px 30px rgba(29, 38, 32, 0.15);
}

.loading-spinner {
  width: 48px;
  height: 48px;
  margin: 0 auto 24px;
  border: 4px solid #E8E3D8;
  border-top-color: #6B6359;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.callback-card h1 {
  margin: 0 0 12px;
  font-size: 24px;
  color: #1D2620;
  font-weight: 600;
}

.status-message {
  color: #6B6359;
  font-size: 14px;
  margin: 0;
}

.error-message {
  color: #C41E3A;
  font-size: 14px;
  margin: 0;
  background: #FFE8E8;
  padding: 12px;
  border-radius: 8px;
  border-left: 4px solid #C41E3A;
}
</style>