<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { loadAlbum, loadAlbumPhotos, type AlbumDetails, type MediaSummary } from '../services/media'

const route = useRoute()
const albumId = computed(() => String(route.params.albumId ?? ''))

const album = ref<AlbumDetails | null>(null)
const photos = ref<MediaSummary[]>([])
const loading = ref(false)
const error = ref<string | null>(null)

async function refresh() {
  if (!albumId.value) {
    error.value = 'Album id is missing'
    return
  }

  loading.value = true
  error.value = null

  try {
    const [albumResponse, photosResponse] = await Promise.all([
      loadAlbum(albumId.value),
      loadAlbumPhotos(albumId.value),
    ])

    album.value = albumResponse
    photos.value = photosResponse
  } catch (exception) {
    error.value = exception instanceof Error ? exception.message : 'Failed to load album photos'
  } finally {
    loading.value = false
  }
}

onMounted(refresh)
watch(albumId, refresh)
</script>

<template>
  <section class="page">
    <header class="page-header">
      <div>
        <p class="eyebrow">Album</p>
        <h1>{{ album?.name ?? 'Album photos' }}</h1>
        <p v-if="album?.description">{{ album.description }}</p>
      </div>
      <button type="button" @click="refresh" :disabled="loading">Refresh</button>
    </header>

    <p v-if="loading">Loading album photos...</p>
    <p v-else-if="error" class="error">{{ error }}</p>
    <p v-else-if="photos.length === 0">No photos in this album yet.</p>

    <div v-else class="photo-grid">
      <article v-for="photo in photos" :key="photo.mediaId" class="photo-card">
        <div class="photo-preview">
          <span>{{ photo.displayName }}</span>
        </div>
        <div class="photo-meta">
          <h2>{{ photo.displayName }}</h2>
          <p>{{ photo.originalFileName }}</p>
          <small>{{ photo.kind }} · {{ photo.safetyStatus }}</small>
        </div>
      </article>
    </div>
  </section>
</template>