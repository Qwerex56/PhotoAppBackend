import { mediaApi } from './api'

export type AlbumSummary = {
  albumId: string
  ownerId: string
  name: string
  description: string | null
  createdAt: string
  updatedAt: string
  visibility: string
}

export type AlbumDetails = AlbumSummary & {
  mediaCount: number
  isDeleted: boolean
}

export type AlbumsResponse = {
  owned: AlbumSummary[]
  shared: AlbumSummary[]
}

export type MediaSummary = {
  mediaId: string
  albumId: string
  ownerId: string
  displayName: string
  originalFileName: string
  contentType: string
  fileSize: number
  checksum: string
  kind: string
  safetyStatus: string
  uploadedAt: string
  updatedAt: string
  tags: string[]
}

export type PhotoItem = MediaSummary & {
  albumName: string
  albumVisibility: string
}

export type MediaDetails = MediaSummary & {
  storageKey: string
  contentType: string
}

export type AlbumWithMedia = {
  album: AlbumSummary
  photos: MediaSummary[]
}

export async function loadAlbums() {
  const response = await mediaApi.get<AlbumsResponse>('/api/albums')
  return response.data
}

export async function loadAlbum(albumId: string) {
  const response = await mediaApi.get<AlbumDetails>(`/api/albums/${albumId}`)
  return response.data
}

export async function loadAlbumPhotos(albumId: string) {
  const response = await mediaApi.get<MediaSummary[]>(`/api/albums/${albumId}/media`)
  return response.data
}

export async function loadMediaDetails(mediaId: string) {
  const response = await mediaApi.get<MediaDetails>(`/api/media/${mediaId}`)
  return response.data
}

export async function loadMediaContent(mediaId: string) {
  const response = await mediaApi.get<Blob>(`/api/media/${mediaId}/content`, {
    responseType: 'blob',
  })
  return response.data
}

export async function loadMediaThumbnail(mediaId: string) {
  // For now reuse the same content endpoint. In future we can introduce
  // a dedicated thumbnail endpoint to reduce bandwidth.
  const response = await mediaApi.get<Blob>(`/api/media/${mediaId}/content`, {
    responseType: 'blob',
  })
  return response.data
}

export async function shareAlbum(albumId: string, sharedWithEmail: string, permission: 'view' | 'edit', expiresAt?: string) {
  await mediaApi.post(`/api/albums/${albumId}/share`, {
    sharedWithEmail,
    permission,
    expiresAt: expiresAt ?? null,
  })
}

export async function loadAllPhotos(): Promise<PhotoItem[]> {
  const albumsResponse = await loadAlbums()
  const albums = [...albumsResponse.owned, ...albumsResponse.shared]

  const collections = await Promise.all(
    albums.map(async (album) => {
      const photos = await loadAlbumPhotos(album.albumId)
      return photos.map((photo) => ({
        ...photo,
        albumName: album.name,
        albumVisibility: album.visibility,
      }))
    }),
  )

  return collections.flat().sort((left, right) => right.uploadedAt.localeCompare(left.uploadedAt))
}