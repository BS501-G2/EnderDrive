import type { ServerSideContext } from './client'

export function createSyncContext(server: ServerSideContext) {
  const { openStream, closeStream, createFolder } = server
}
