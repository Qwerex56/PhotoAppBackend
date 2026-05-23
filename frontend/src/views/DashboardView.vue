<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { mediaApi } from '../services/api'
import { loadAlbums, type AlbumSummary, type AlbumsResponse } from '../services/media'

const authStore = useAuthStore()

const albumName = ref('')
const albumDescription = ref('')
const selectedAlbumId = ref('')
const uploadFile = ref<File | null>(null)
const albums = ref<AlbumsResponse | null>(null)
const status = ref<string | null>(null)
const error = ref<string | null>(null)
const loading = ref(false)

const allAlbums = computed(() => {
  const owned = albums.value?.owned ?? []
  const shared = albums.value?.shared ?? []
  return [...owned, ...shared]
})

function selectAlbumForUpload(album: AlbumSummary) {
  selectedAlbumId.value = album.albumId
}

async function createAlbum() {
  loading.value = true
  error.value = null
  status.value = null

  try {
    const response = await mediaApi.post('/api/albums', {
      name: albumName.value,
      description: albumDescription.value,
    })

    status.value = `Album created: ${response.data.albumId ?? 'ok'}`
    albumName.value = ''
    albumDescription.value = ''
    await loadAlbums()
  } catch (exception) {
    error.value = exception instanceof Error ? exception.message : 'Album creation failed'
  } finally {
    loading.value = false
  }
}

async function uploadPhoto() {
  if (!selectedAlbumId.value || !uploadFile.value) {
    error.value = 'Choose an album and a file first'
    return
  }

  loading.value = true
  error.value = null
  status.value = null

  try {
    const checksum = await computeChecksum(uploadFile.value)
    const formData = new FormData()
    formData.append('file', uploadFile.value)
    formData.append('displayName', uploadFile.value.name)
    formData.append('checksum', checksum)

    await mediaApi.post(`/api/albums/${selectedAlbumId.value}/media`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })

    status.value = 'Photo uploaded'
    uploadFile.value = null
    selectedAlbumId.value = ''
    await loadAlbums()
  } catch (exception) {
    error.value = exception instanceof Error ? exception.message : 'Upload failed'
  } finally {
    loading.value = false
  }
}

function handleFileChange(event: Event) {
  const input = event.target as HTMLInputElement
  uploadFile.value = input.files?.[0] ?? null
}

function formatAlbumSubtitle(album: AlbumSummary) {
  const visibility = album.visibility.charAt(0).toUpperCase() + album.visibility.slice(1)
  return album.description?.trim() || visibility
}

async function computeChecksum(file: File): Promise<string> {
  const buffer = await file.arrayBuffer()
  const digest = await crypto.subtle.digest('SHA-256', buffer)
  const bytes = Array.from(new Uint8Array(digest))
  return bytes.map((byte) => byte.toString(16).padStart(2, '0')).join('')
}

onMounted(async () => {
  try {
    await loadAlbums()
  } catch (exception) {
    error.value = exception instanceof Error ? exception.message : 'Failed to load albums'
  }
})
</script>

<template>
  <section>
    <h1>Demo app</h1>
    <p v-if="authStore.email">Logged in as {{ authStore.email }}</p>

    <section>
      <h2>Create album</h2>
      <form @submit.prevent="createAlbum">
        <label>
          Name
          <input v-model="albumName" type="text" required />
        </label>

        <label>
          Description
          <input v-model="albumDescription" type="text" />
        </label>

        <button type="submit" :disabled="loading">Create album</button>
      </form>
    </section>

    <section>
      <h2>Upload photo</h2>
      <label>
        Album
        <select v-model="selectedAlbumId">
          <option value="">Select album</option>
          <option v-for="album in allAlbums" :key="album.albumId" :value="album.albumId">
            {{ album.name }}
          </option>
        </select>
      </label>

      <label>
        File
        <input type="file" accept="image/*" @change="handleFileChange" />
      </label>

      <button type="button" :disabled="loading" @click="uploadPhoto">Upload photo</button>
    </section>

    <section>
      <h2>Albums</h2>
      <div class="section-actions">
        <button type="button" @click="loadAlbums">Refresh</button>
        <RouterLink to="/photos">View all photos</RouterLink>
      </div>

      <div v-if="allAlbums.length === 0" class="empty-state">
        No albums yet.
      </div>

      <div v-else class="album-grid">
        <article v-for="album in allAlbums" :key="album.albumId" class="album-card">
          <div>
            <p class="album-visibility">{{ album.visibility }}</p>
            <h3>{{ album.name }}</h3>
            <p>{{ formatAlbumSubtitle(album) }}</p>
          </div>

          <div class="album-card-actions">
            <button type="button" @click="selectAlbumForUpload(album)">Use for upload</button>
            <RouterLink :to="`/albums/${album.albumId}/photos`">Open photos</RouterLink>
          </div>
        </article>
      </div>
    </section>

    <p v-if="status">{{ status }}</p>
    <p v-if="error">{{ error }}</p>
  </section>
</template>