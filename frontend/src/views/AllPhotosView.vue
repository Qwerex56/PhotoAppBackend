<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { loadAllPhotos, type PhotoItem } from '../services/media'

const photos = ref<PhotoItem[]>([])
const loading = ref(false)
const error = ref<string | null>(null)

async function refresh() {
  loading.value = true
  error.value = null

  try {
    photos.value = await loadAllPhotos()
  } catch (exception) {
    error.value = exception instanceof Error ? exception.message : 'Failed to load photos'
  } finally {
    loading.value = false
  }
}

onMounted(refresh)
</script>

<template>
  <section class="page">
    <header class="page-header">
      <div>
        <p class="eyebrow">Photos</p>
        <h1>All photos</h1>
      </div>
      <button type="button" @click="refresh" :disabled="loading">Refresh</button>
    </header>

    <p v-if="loading">Loading photos...</p>
    <p v-else-if="error" class="error">{{ error }}</p>
    <p v-else-if="photos.length === 0">No photos yet.</p>

    <div v-else class="photo-grid">
      <article v-for="photo in photos" :key="photo.mediaId" class="photo-card">
        <div class="photo-preview">
          <span>{{ photo.displayName }}</span>
        </div>
        <div class="photo-meta">
          <h2>{{ photo.displayName }}</h2>
          <p>{{ photo.albumName }}</p>
          <small>{{ photo.originalFileName }}</small>
        </div>
      </article>
    </div>
  </section>
</template>