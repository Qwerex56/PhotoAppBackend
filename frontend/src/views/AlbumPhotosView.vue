<script setup lang="ts">
import { computed, onMounted, ref, watch, onUnmounted } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import {
  loadAlbum,
  loadAlbumPhotos,
  loadMediaContent,
  loadMediaThumbnail,
  loadMediaDetails,
  shareAlbum,
  type AlbumDetails,
  type MediaDetails,
  type MediaSummary,
} from '../services/media'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const albumId = computed(() => String(route.params.albumId ?? ''))

const album = ref<AlbumDetails | null>(null)
const photos = ref<MediaSummary[]>([])
const loading = ref(false)
const error = ref<string | null>(null)
const previewVisible = ref(false)
const previewLoading = ref(false)
const previewError = ref<string | null>(null)
const selectedPhoto = ref<MediaDetails | null>(null)
const previewUrl = ref<string | null>(null)
const thumbnails = ref<Record<string, string>>({})

const shareEmail = ref('')
const sharePermission = ref<'view' | 'edit'>('view')
const shareExpiresAt = ref('')
const shareStatus = ref<string | null>(null)
const shareError = ref<string | null>(null)
const sharing = ref(false)

async function refresh() {
  if (!albumId.value) {
    error.value = 'ID albumu jest brakujący'
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
      await loadThumbnails()
  } catch (exception) {
    error.value = exception instanceof Error ? exception.message : 'Nie udało się załadować zdjęć albumu'
  } finally {
    loading.value = false
  }
}

async function loadThumbnails() {
  // cleanup previous urls
  Object.values(thumbnails.value).forEach((u) => {
    try { URL.revokeObjectURL(u) } catch {}
  })
  thumbnails.value = {}

  await Promise.all(
    photos.value.map(async (p) => {
      try {
        const blob = await loadMediaThumbnail(p.mediaId)
        thumbnails.value[p.mediaId] = URL.createObjectURL(blob)
      } catch {
        // ignore individual thumbnail failures
      }
    }),
  )
}

function closePreview() {
  previewVisible.value = false
  selectedPhoto.value = null
  previewError.value = null

  if (previewUrl.value) {
    URL.revokeObjectURL(previewUrl.value)
    previewUrl.value = null
  }
}

async function openPreview(photo: MediaSummary) {
  previewVisible.value = true
  previewError.value = null
  previewLoading.value = true

  try {
    const [details, contentBlob] = await Promise.all([
      loadMediaDetails(photo.mediaId),
      loadMediaContent(photo.mediaId),
    ])

    selectedPhoto.value = details
    previewUrl.value = URL.createObjectURL(contentBlob)
  } catch (exception) {
    previewError.value = exception instanceof Error ? exception.message : 'Nie udało się wczytać podglądu zdjęcia'
  } finally {
    previewLoading.value = false
  }
}

async function shareAlbumWithUser() {
  if (!albumId.value || !shareEmail.value.trim()) {
    shareError.value = 'Wprowadź email użytkownika'
    shareStatus.value = null
    return
  }

  sharing.value = true
  shareError.value = null
  shareStatus.value = null

  try {
    await shareAlbum(albumId.value, shareEmail.value.trim(), sharePermission.value, shareExpiresAt.value || undefined)
    shareStatus.value = 'Użytkownik został dodany do albumu'
    shareEmail.value = ''
    shareExpiresAt.value = ''
  } catch (exception) {
    shareError.value = exception instanceof Error ? exception.message : 'Nie udało się udostępnić albumu'
  } finally {
    sharing.value = false
  }
}

async function logout() {
  await authStore.logout()
  await router.push('/login')
}

onMounted(refresh)
watch(albumId, refresh)
onUnmounted(() => {
  Object.values(thumbnails.value).forEach((u) => {
    try { URL.revokeObjectURL(u) } catch {}
  })
  thumbnails.value = {}
})
</script>

<template>
  <div class="album-container">
    <!-- Header -->
    <header class="album-header">
      <div class="header-content">
        <div class="header-left">
          <RouterLink to="/app" class="btn-back">← Wróć</RouterLink>
          <div class="header-title">
            <h1>{{ album?.name ?? 'Zdjęcia albumu' }}</h1>
            <p v-if="album?.description" class="header-desc">{{ album.description }}</p>
          </div>
        </div>
        <div class="header-actions">
          <span class="user-info">{{ authStore.email }}</span>
          <button @click="logout" class="btn btn-logout">Wyloguj się</button>
        </div>
      </div>
    </header>

    <!-- Main Content -->
    <main class="album-main">
      <div class="album-toolbar">
        <button @click="refresh" :disabled="loading" class="btn btn-secondary">
          🔄 {{ loading ? 'Ładowanie...' : 'Odśwież' }}
        </button>
      </div>

      <section class="card share-card">
        <h2>Udostępnij album</h2>
        <p class="share-note">Dodaj użytkownika do tego albumu jako przeglądającego lub edytującego.</p>
        <form @submit.prevent="shareAlbumWithUser" class="form">
          <div class="form-group">
            <label for="share-user-email">Email użytkownika</label>
            <input
              id="share-user-email"
              v-model="shareEmail"
              type="email"
              placeholder="Wprowadź email użytkownika"
              required
              :disabled="sharing"
            />
          </div>

          <div class="form-group">
            <label for="share-permission">Uprawnienie</label>
            <select
              id="share-permission"
              v-model="sharePermission"
              :disabled="sharing"
            >
              <option value="view">Przeglądający</option>
              <option value="edit">Edytujący</option>
            </select>
          </div>

          <div class="form-group">
            <label for="share-expires-at">Wygasa (opcjonalnie)</label>
            <input
              id="share-expires-at"
              v-model="shareExpiresAt"
              type="date"
              :disabled="sharing"
            />
          </div>

          <button type="submit" :disabled="sharing" class="btn btn-primary">
            {{ sharing ? 'Udostępnianie...' : 'Dodaj użytkownika' }}
          </button>

          <p v-if="shareStatus" class="message message-success small-message">{{ shareStatus }}</p>
          <p v-if="shareError" class="message message-error small-message">{{ shareError }}</p>
        </form>
      </section>

      <div v-if="error" class="message message-error">
        {{ error }}
      </div>

      <div v-else-if="photos.length === 0" class="empty-state">
        <p>📭 Ten album nie ma jeszcze zdjęć</p>
        <RouterLink to="/app" class="btn btn-primary">
          Prześlij zdjęcie do tego albumu
        </RouterLink>
      </div>

      <div v-else>
        <p class="photos-count">{{ photos.length }} zdjęć w tym albumie</p>
        <div class="photos-grid">
          <article
            v-for="photo in photos"
            :key="photo.mediaId"
            class="photo-card"
            @click="openPreview(photo)"
          >
            <div class="photo-placeholder">
              <img v-if="thumbnails[photo.mediaId]" :src="thumbnails[photo.mediaId]" class="photo-thumb" />
              <span v-else-if="photo.kind === 'video'">🎬</span>
              <span v-else>📷</span>
            </div>
            <div class="photo-info">
              <h3 class="photo-title">{{ photo.displayName }}</h3>
              <p class="photo-meta">
                {{ photo.kind === 'video' ? 'Wideo' : 'Zdjęcie' }}
              </p>
              <p class="photo-filename">{{ photo.originalFileName }}</p>
              <button type="button" class="btn btn-secondary-small preview-button">
                Podgląd
              </button>
            </div>
          </article>
        </div>
      </div>

      <div v-if="previewVisible" class="preview-overlay" @click.self="closePreview">
        <div class="preview-dialog">
          <button type="button" class="btn btn-close" @click="closePreview">✕</button>

          <div class="preview-content">
            <div v-if="previewLoading" class="preview-loading">
              Ładowanie podglądu...
            </div>

            <div v-else-if="previewError" class="message message-error">
              {{ previewError }}
            </div>

            <div v-else-if="selectedPhoto" class="preview-details">
              <div class="preview-hero">
                <div class="preview-image">
                <template v-if="selectedPhoto.kind === 'video' && previewUrl">
                  <video controls class="preview-media">
                    <source :src="previewUrl" :type="selectedPhoto.contentType" />
                    Twoja przeglądarka nie obsługuje elementu wideo.
                  </video>
                </template>
                <template v-else-if="previewUrl">
                  <img class="preview-media" :src="previewUrl" :alt="selectedPhoto.displayName" />
                </template>
                <template v-else>
                  <span v-if="selectedPhoto.kind === 'video'">🎬</span>
                  <span v-else>📷</span>
                </template>
              </div>
              <div class="preview-meta">
                  <h2>{{ selectedPhoto.displayName }}</h2>
                  <p class="preview-type">{{ selectedPhoto.kind === 'video' ? 'Wideo' : 'Zdjęcie' }}</p>
                  <p>{{ selectedPhoto.originalFileName }}</p>
                  <p>Rozmiar: {{ selectedPhoto.fileSize }} bajtów</p>
                  <p>Bezpieczeństwo: {{ selectedPhoto.safetyStatus }}</p>
                  <p>Przesłane: {{ new Date(selectedPhoto.uploadedAt).toLocaleString() }}</p>
                </div>
              </div>

              <div class="preview-tags" v-if="selectedPhoto.tags.length > 0">
                <strong>Tagi:</strong>
                <span v-for="tag in selectedPhoto.tags" :key="tag" class="tag">{{ tag }}</span>
              </div>

              <div class="preview-notice">
                Jeśli potrzebujesz pełnego podglądu obrazu, dodaj w backendzie endpoint serwujący zawartość pliku.
              </div>
            </div>
          </div>
        </div>
      </div>
    </main>
  </div>
</template>

<style scoped>
.album-container {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  background: #F5F1ED;
}

.album-header {
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

.header-left {
  display: flex;
  align-items: center;
  gap: 20px;
  flex: 1;
}

.btn-back {
  background: rgba(255, 255, 255, 0.2);
  color: white;
  padding: 8px 16px;
  border-radius: 6px;
  text-decoration: none;
  font-size: 14px;
  border: 1px solid rgba(255, 255, 255, 0.3);
  transition: all 0.2s ease;
  white-space: nowrap;
}

.btn-back:hover {
  background: rgba(255, 255, 255, 0.3);
}

.header-title h1 {
  margin: 0;
  font-size: 28px;
  font-weight: 600;
}

.header-desc {
  margin: 8px 0 0;
  font-size: 14px;
  opacity: 0.9;
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

.album-main {
  flex: 1;
  max-width: 1200px;
  margin: 0 auto;
  width: 100%;
  padding: 32px 24px;
}

.album-toolbar {
  display: flex;
  gap: 12px;
  margin-bottom: 24px;
}

.share-card {
  background: linear-gradient(180deg, #fffaf4 0%, #fff3ea 100%);
  border: 1px solid rgba(179, 143, 99, 0.28);
  border-radius: 24px;
  padding: 28px;
  box-shadow: 0 18px 45px rgba(88, 75, 66, 0.08);
  margin-bottom: 24px;
}

.share-card h2 {
  margin: 0 0 8px;
  font-size: 20px;
  color: #1d2620;
}

.form {
  display: grid;
  gap: 18px;
}

.form-group {
  display: grid;
  gap: 8px;
}

.form-group label {
  font-weight: 600;
  color: #534a3d;
}

.form-group input,
.form-group select {
  width: 100%;
  padding: 14px 16px;
  border: 1px solid #d5c7b7;
  border-radius: 14px;
  background: white;
  color: #1d2620;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}

.form-group input:focus,
.form-group select:focus {
  outline: none;
  border-color: #6b6359;
  box-shadow: 0 0 0 4px rgba(107, 99, 89, 0.08);
}

.btn-primary {
  background: linear-gradient(135deg, #6b6359 0%, #3f362c 100%);
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background: linear-gradient(135deg, #584b42 0%, #3a3028 100%);
}

.preview-media {
  width: 100%;
  height: 100%;
  object-fit: contain;
  border-radius: 18px;
}

.preview-image {
  min-height: 220px;
  background: linear-gradient(135deg, #e8e3d8 0%, #d5ccc1 100%);
  border-radius: 18px;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  position: relative;
}

.share-note {
  margin: 0 0 16px;
  color: #6b6359;
  font-size: 14px;
}

.small-message {
  margin-top: 12px;
  font-size: 13px;
}

.preview-button {
  margin-top: 12px;
  width: fit-content;
}

.preview-overlay {
  position: fixed;
  inset: 0;
  background: rgba(29, 38, 32, 0.65);
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 24px;
  z-index: 60;
}

.preview-dialog {
  position: relative;
  width: min(100%, 940px);
  max-height: 90vh;
  overflow-y: auto;
  background: white;
  border-radius: 24px;
  padding: 28px;
  box-shadow: 0 18px 60px rgba(29, 38, 32, 0.25);
}

.btn-close {
  position: absolute;
  top: 18px;
  right: 18px;
  border: none;
  background: transparent;
  color: #1d2620;
  font-size: 22px;
  cursor: pointer;
}

.preview-content {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.preview-loading,
.preview-notice {
  color: #6b6359;
}

.preview-details {
  display: grid;
  gap: 20px;
}

.preview-hero {
  display: grid;
  grid-template-columns: 220px 1fr;
  gap: 24px;
  align-items: start;
}

.preview-image {
  height: 220px;
  background: linear-gradient(135deg, #e8e3d8 0%, #d5ccc1 100%);
  border-radius: 18px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 64px;
}

.preview-meta h2 {
  margin: 0 0 10px;
  font-size: 24px;
  color: #1d2620;
}

.preview-meta p {
  margin: 6px 0;
  color: #6b6359;
  font-size: 14px;
}

.preview-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
}

.tag {
  background: #f5f1ed;
  border: 1px solid #c4baa8;
  border-radius: 999px;
  padding: 6px 12px;
  font-size: 13px;
  color: #1d2620;
}

.preview-notice {
  padding: 16px;
  border-radius: 12px;
  background: #f9f5ef;
  border: 1px solid #e0d7ce;
}

.message {
  padding: 16px;
  border-radius: 8px;
  margin-bottom: 24px;
  font-weight: 500;
}

.message-error {
  background: #FFE8E8;
  color: #C41E3A;
  border-left: 4px solid #C41E3A;
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

.photos-count {
  color: #6B6359;
  font-size: 14px;
  margin-bottom: 20px;
}

.photos-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  gap: 20px;
}

.photo-card {
  background: white;
  border: 1px solid #E0D7CE;
  border-radius: 12px;
  overflow: hidden;
  transition: all 0.2s ease;
  display: flex;
  flex-direction: column;
}

.photo-card:hover {
  border-color: #6B6359;
  box-shadow: 0 4px 12px rgba(107, 99, 89, 0.15);
  transform: translateY(-2px);
}

.photo-placeholder {
  height: 160px;
  background: linear-gradient(135deg, #E8E3D8 0%, #D5CCC1 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 48px;
}

.photo-thumb {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}

.photo-info {
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  flex: 1;
}

.photo-title {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
  color: #1D2620;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.photo-meta {
  margin: 0;
  font-size: 12px;
  color: #9B8F7E;
  display: flex;
  gap: 8px;
}

.photo-filename {
  margin: 0;
  font-size: 12px;
  color: #9B8F7E;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
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
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
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

.btn-primary {
  background: #6B6359;
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background: #584B42;
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

@media (max-width: 768px) {
  .album-header {
    padding: 16px;
  }

  .header-content {
    flex-direction: column;
    align-items: flex-start;
  }

  .header-left {
    flex-direction: column;
    gap: 12px;
  }

  .header-title h1 {
    font-size: 24px;
  }

  .album-main {
    padding: 20px 16px;
  }

  .photos-grid {
    grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
    gap: 12px;
  }

  .photo-placeholder {
    height: 120px;
    font-size: 36px;
  }

  .photo-info {
    padding: 12px;
  }
}
</style>