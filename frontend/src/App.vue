<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
import { useAuthStore } from './stores/auth'

const authStore = useAuthStore()
</script>

<template>
  <div>
    <nav>
      <RouterLink to="/app">Dashboard</RouterLink>
      <RouterLink to="/photos">All photos</RouterLink>
      <RouterLink to="/login">Login</RouterLink>
      <RouterLink to="/register">Register</RouterLink>
      <button v-if="authStore.isAuthenticated" type="button" @click="authStore.logout()">
        Logout
      </button>
    </nav>

    <RouterView />
  </div>
</template>
