<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { mediaApi } from '../services/api'
import { loadAlbums, type AlbumSummary } from '../services/media'

const router = useRouter()
const authStore = useAuthStore()

const albumName = ref('')
const albumDescription = ref('')
const selectedAlbumId = ref('')
const uploadFile = ref<File | null>(null)
const ownedAlbums = ref<AlbumSummary[]>([])
const sharedAlbums = ref<AlbumSummary[]>([])
const status = ref<string | null>(null)
const error = ref<string | null>(null)
const loading = ref(false)
const loadingAlbums = ref(false)
const showCreateAlbum = ref(false)
const showUploadPhoto = ref(false)

const allAlbums = computed(() => [...ownedAlbums.value, ...sharedAlbums.value])

function normalizeAlbum(album: Partial<AlbumSummary> | null | undefined): AlbumSummary {
  const safeAlbum = album ?? {}
  const rawVisibility = String(safeAlbum.visibility ?? (safeAlbum as any).Visibility ?? 'private').toLowerCase()
  const visibility = rawVisibility === 'public' ? 'public' : 'private'

  return {
    albumId: String(safeAlbum.albumId ?? (safeAlbum as any).AlbumId ?? ''),
    ownerId: String(safeAlbum.ownerId ?? (safeAlbum as any).OwnerId ?? ''),
    name: String(safeAlbum.name ?? (safeAlbum as any).Name ?? 'Album'),
    description: safeAlbum.description ?? (safeAlbum as any).Description ?? null,
    createdAt: String(safeAlbum.createdAt ?? (safeAlbum as any).CreatedAt ?? ''),
    updatedAt: String(safeAlbum.updatedAt ?? (safeAlbum as any).UpdatedAt ?? ''),
    visibility,
  }
}

function selectAlbumForUpload(album: AlbumSummary) {
  selectedAlbumId.value = album.albumId
}

function navigateToAlbum(albumId: string) {
  router.push(`/albums/${albumId}/photos`)
}

async function fetchAlbums() {
  loadingAlbums.value = true
  error.value = null

  try {
    const albumsResponse = await loadAlbums()
    ownedAlbums.value = Array.isArray(albumsResponse.owned)
      ? albumsResponse.owned.map(normalizeAlbum)
      : []
    sharedAlbums.value = Array.isArray(albumsResponse.shared)
      ? albumsResponse.shared.map(normalizeAlbum)
      : []
  } catch (exception) {
    error.value = exception instanceof Error
      ? exception.message
      : 'Nie udało się pobrać albumów'
  } finally {
    loadingAlbums.value = false
  }
}

async function createAlbum() {
  loading.value = true
  error.value = null
  status.value = null

  try {
    const response = await mediaApi.post('/api/albums', {
      Name: albumName.value,
      Description: albumDescription.value,
    })

    status.value = `Album created: ${response.data.albumId ?? 'ok'}`
    albumName.value = ''
    albumDescription.value = ''
    await fetchAlbums()
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
    await fetchAlbums()
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
  const visibility = String(album.visibility ?? 'private').trim().toLowerCase()
  const label = visibility === 'public' ? 'Publiczny' : 'Prywatny'
  return album.description?.trim() || label
}

async function computeChecksum(file: File): Promise<string> {
  const buffer = await file.arrayBuffer()
  const digest = await crypto.subtle.digest('SHA-256', buffer)
  const bytes = Array.from(new Uint8Array(digest))
  return bytes.map((byte) => byte.toString(16).padStart(2, '0')).join('')
}

async function logout() {
  await authStore.logout()
  await router.push('/login')
}

onMounted(fetchAlbums)
</script>

<template>
  <div class="dashboard-container">
    <!-- Header -->
    <header class="dashboard-header">
      <div class="header-content">
        <h1>📸 PhotoApp</h1>
        <div class="header-actions">
          <span class="user-info">{{ authStore.email }}</span>
          <button @click="logout" class="btn btn-logout">Wyloguj się</button>
        </div>
      </div>
    </header>

    <!-- Main Content -->
    <main class="dashboard-main">
      <!-- Status Messages -->
      <div v-if="status" class="message message-success">
        {{ status }}
      </div>
      <div v-if="error" class="message message-error">
        {{ error }}
      </div>

      <!-- Action Buttons -->
      <div class="action-buttons">
        <button
          @click="showCreateAlbum = !showCreateAlbum"
          class="btn btn-primary"
        >
          + Nowy album
        </button>
        <button
          @click="showUploadPhoto = !showUploadPhoto"
          class="btn btn-secondary"
        >
          ↑ Prześlij zdjęcie
        </button>
        <RouterLink to="/photos" class="btn btn-secondary">
          👁️ Wszystkie zdjęcia
        </RouterLink>
      </div>

      <!-- Create Album Section -->
      <section v-if="showCreateAlbum" class="card">
        <h2>Utwórz nowy album</h2>
        <form @submit.prevent="createAlbum" class="form">
          <div class="form-group">
            <label for="album-name">Nazwa albumu</label>
            <input
              id="album-name"
              v-model="albumName"
              type="text"
              placeholder="Moje wakacje..."
              required
              :disabled="loading"
            />
          </div>

          <div class="form-group">
            <label for="album-desc">Opis (opcjonalnie)</label>
            <input
              id="album-desc"
              v-model="albumDescription"
              type="text"
              placeholder="Krótki opis albumu"
              :disabled="loading"
            />
          </div>

          <button type="submit" :disabled="loading" class="btn btn-primary">
            {{ loading ? 'Tworzenie...' : 'Utwórz album' }}
          </button>
        </form>
      </section>

      <!-- Upload Photo Section -->
      <section v-if="showUploadPhoto" class="card">
        <h2>Prześlij zdjęcie</h2>
        <form @submit.prevent="uploadPhoto" class="form">
          <div class="form-group">
            <label for="album-select">Wybierz album</label>
            <select
              id="album-select"
              v-model="selectedAlbumId"
              required
              :disabled="loading"
            >
              <option value="">-- Wybierz album --</option>
              <option v-for="album in allAlbums" :key="album.albumId" :value="album.albumId">
                {{ album.name }}
              </option>
            </select>
          </div>

          <div class="form-group">
            <label for="file-input">Plik (zdjęcie)</label>
            <input
              id="file-input"
              type="file"
              accept="image/*"
              @change="handleFileChange"
              :disabled="loading"
            />
            <span v-if="uploadFile" class="file-name">
              📄 {{ uploadFile.name }}
            </span>
          </div>

          <button type="button" :disabled="loading" @click="uploadPhoto" class="btn btn-primary">
            {{ loading ? 'Przesyłanie...' : 'Prześlij zdjęcie' }}
          </button>
        </form>
      </section>

      <!-- Albums Section -->
      <section class="card">
        <div class="section-header">
          <h2>Moje albumy</h2>
          <button @click="fetchAlbums" class="btn btn-text">
            🔄 {{ loadingAlbums ? 'Odświeżanie...' : 'Odśwież' }}
          </button>
        </div>

        <div v-if="loadingAlbums" class="empty-state">
          <p>Ładowanie albumów...</p>
        </div>

        <div v-else-if="allAlbums.length === 0" class="empty-state">
          <p>📭 Nie masz jeszcze żadnych albumów</p>
          <button @click="showCreateAlbum = true" class="btn btn-primary">
            Utwórz pierwszy album
          </button>
        </div>

        <div v-else class="albums-grid">
          <article
            v-for="album in allAlbums"
            :key="album.albumId"
            class="album-card"
          >
            <div class="album-header">
              <span class="album-badge">{{ album.visibility === 'private' ? '🔒' : '👥' }}</span>
              <h3>{{ album.name }}</h3>
              <button
                @click="navigateToAlbum(album.albumId)"
                class="btn btn-secondary-small"
              >
                Zobacz zdjęcia
              </button>
            </div>

            <p class="album-description">
              {{ formatAlbumSubtitle(album) }}
            </p>

            <div class="album-actions">
              <button
                @click="selectAlbumForUpload(album)"
                class="btn btn-secondary-small"
              >
                Prześlij do tego
              </button>
            </div>
          </article>
        </div>
      </section>
    </main>
  </div>
</template>

<style scoped>
.dashboard-container {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  background: #F5F1ED;
}

.dashboard-header {
  background: linear-gradient(135deg, #6B6359 0%, #584B42 100%);
  color: white;
  padding: 24px;
  box-shadow: 0 2px 8px rgba(29, 38, 32, 0.1);
}

.header-content {
  max-width: 1200px;
  margin: 0 auto;
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 20px;
}

.dashboard-header h1 {
  margin: 0;
  font-size: 28px;
  font-weight: 600;
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 20px;
}

.user-info {
  font-size: 14px;
  opacity: 0.9;
}

.dashboard-main {
  flex: 1;
  max-width: 1200px;
  margin: 0 auto;
  width: 100%;
  padding: 32px 24px;
}

.message {
  padding: 16px;
  border-radius: 8px;
  margin-bottom: 24px;
  font-weight: 500;
}

.message-success {
  background: #E6F9F0;
  color: #2D5016;
  border-left: 4px solid #2D5016;
}

.message-error {
  background: #FFE8E8;
  color: #C41E3A;
  border-left: 4px solid #C41E3A;
}

.action-buttons {
  display: flex;
  gap: 12px;
  margin-bottom: 32px;
  flex-wrap: wrap;
}

.card {
  background: white;
  border-radius: 12px;
  padding: 32px;
  margin-bottom: 24px;
  box-shadow: 0 1px 3px rgba(29, 38, 32, 0.1);
  border: 1px solid #E0D7CE;
}

.card h2 {
  margin-top: 0;
  margin-bottom: 24px;
  color: #1D2620;
  font-size: 22px;
}

.form {
  display: flex;
  flex-direction: column;
  gap: 16px;
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

.form-group input,
.form-group select {
  padding: 12px 16px;
  border: 1px solid #C4BAA8;
  border-radius: 8px;
  font-size: 16px;
  font-family: var(--sans);
  background: #F5F1ED;
  color: #1D2620;
  transition: all 0.2s ease;
}

.form-group input:focus,
.form-group select:focus {
  outline: none;
  border-color: #6B6359;
  background: white;
  box-shadow: 0 0 0 3px rgba(107, 99, 89, 0.1);
}

.form-group input:disabled,
.form-group select:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.file-name {
  font-size: 12px;
  color: #6B6359;
  margin-top: -4px;
}

.btn {
  padding: 12px 16px;
  border: none;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
  font-family: var(--sans);
  text-decoration: none;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  white-space: nowrap;
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-primary {
  background: #6B6359;
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background: #584B42;
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(107, 99, 89, 0.2);
}

.btn-secondary {
  background: white;
  color: #6B6359;
  border: 1px solid #C4BAA8;
}

.btn-secondary:hover:not(:disabled) {
  background: #F5F1ED;
  border-color: #6B6359;
}

.btn-secondary-small {
  padding: 8px 12px;
  font-size: 12px;
  background: #E8E3D8;
  color: #1D2620;
}

.btn-secondary-small:hover:not(:disabled) {
  background: #D5CCC1;
}

.btn-text {
  background: transparent;
  color: #6B6359;
  font-size: 12px;
  padding: 6px 12px;
}

.btn-text:hover:not(:disabled) {
  color: #1D2620;
}

.btn-logout {
  background: rgba(255, 255, 255, 0.2);
  color: white;
  border: 1px solid rgba(255, 255, 255, 0.3);
  padding: 8px 16px;
  font-size: 13px;
}

.btn-logout:hover:not(:disabled) {
  background: rgba(255, 255, 255, 0.3);
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
}

.section-header h2 {
  margin: 0;
}

.empty-state {
  text-align: center;
  padding: 64px 24px;
  color: #6B6359;
}

.empty-state p {
  font-size: 18px;
  margin-bottom: 24px;
}

.albums-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 20px;
}

.album-card {
  border: 1px solid #E0D7CE;
  border-radius: 12px;
  padding: 20px;
  background: #F5F1ED;
  transition: all 0.2s ease;
}

.album-card:hover {
  border-color: #6B6359;
  box-shadow: 0 4px 12px rgba(107, 99, 89, 0.15);
}

.album-header {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 12px;
}

.album-badge {
  font-size: 20px;
  line-height: 1;
}

.album-card h3 {
  margin: 0;
  font-size: 18px;
  color: #1D2620;
}

.album-description {
  color: #6B6359;
  font-size: 14px;
  margin: 12px 0 16px;
}

.album-actions {
  display: flex;
  gap: 8px;
}

.album-actions .btn {
  flex: 1;
  text-align: center;
}

@media (max-width: 768px) {
  .dashboard-header {
    padding: 16px;
  }

  .header-content {
    flex-direction: column;
    align-items: flex-start;
  }

  .dashboard-header h1 {
    font-size: 24px;
  }

  .dashboard-main {
    padding: 20px 16px;
  }

  .card {
    padding: 20px;
  }

  .action-buttons {
    flex-direction: column;
  }

  .action-buttons .btn {
    width: 100%;
  }

  .albums-grid {
    grid-template-columns: 1fr;
  }

  .album-actions {
    flex-direction: column;
  }

  .album-actions .btn {
    width: 100%;
  }
}
</style>