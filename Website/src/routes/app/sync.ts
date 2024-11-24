/* eslint-disable @typescript-eslint/no-explicit-any */

import { getContext, setContext } from 'svelte'
import Dexie, { type EntityTable } from 'dexie'
import {
  FileLogType,
  FileType,
  type FileResource,
  type ServerSideContext
} from '$lib/client/client'

export type SyncContextState =
  | [type: 'loading']
  | [type: 'error', error: Error]
  | [type: 'ready', database: IDBDatabase, remoteFiles: IDBObjectStore, localFiles: IDBObjectStore]

const name = Symbol('sync')

export type SyncContext = ReturnType<typeof createSyncContext>['context']

export function useSyncContext() {
  return getContext<SyncContext>(name)
}

type Database = Dexie & {
  syncEntries: EntityTable<SyncEntry, 'id'>
  remoteFiles: EntityTable<RemoteFileEntry, 'fileId'>
  localFiles: EntityTable<LocalFileEntry, 'path'>
}

function createDatabase() {
  const database = new Dexie('sync') as Database

  database.version(1).stores({
    syncEntries: '++id, remoteFileId, localFileHandle',
    remoteFiles: 'fileId, fileSnapshotId, parentId, lastModified',
    localFiles: 'filePath, parentFilePath, lastModified'
  })

  return database
}

export function createSyncContext(server: ServerSideContext) {
  const database = createDatabase()

  const context = setContext(name, {})

  const syncJobs: Record<number, JobContext> = {}

  async function initSync() {
    for (const syncEntry of await database.syncEntries.toArray()) {
      void runSyncJob(syncEntry)
    }
  }

  async function runSyncJob(syncEntry: SyncEntry) {
    async function run(context: JobContext) {
      async function* getRemote(
        fileId: string,
        currentPath: string[]
      ): AsyncGenerator<RemoteFileEntry> {
        const file = await server.getFile(fileId)

        if (file.type === FileType.Folder) {
          for (const entry of await server.getFiles(file.id)) {
            yield* getRemote(entry.id, [...currentPath, entry.name])
          }
        } else if (file.type === FileType.File) {
          const fileSnapshot = (await server.getLatestFileSnapshot(file.id))!

          yield {
            fileId: file.id,
            fileSnapshotId: fileSnapshot.id,
            lastModified: new Date(fileSnapshot.createTime),
            path: [...currentPath, file.name]
          }
        }
      }

      async function* getLocal(
        entry: FileSystemEntry,
        currentPath: string[]
      ): AsyncGenerator<LocalFileEntryFresh> {
        if (entry.isDirectory) {
          const [, entries] = await Promise.all([
            new Promise<Metadata>((resolve, reject) =>
              (entry as FileSystemDirectoryEntry).getMetadata(resolve, reject)
            ),
            new Promise<FileSystemEntry[]>((resolve, reject) =>
              (entry as FileSystemDirectoryEntry).createReader().readEntries(resolve, reject)
            )
          ] as const)

          for (const entry of entries) {
            yield* getLocal(entry, [...currentPath, entry.name])
          }
        } else if (entry.isFile) {
          const file = await new Promise<File>((resolve, reject) =>
            (entry as FileSystemFileEntry).file(resolve, reject)
          )

          yield {
            lastModified: new Date(file.lastModified),
            path: [...currentPath, file.name],
            entry: entry as FileSystemFileEntry
          }
        }
      }

      const map: Record<
        string,
        {
          remoteFresh?: RemoteFileEntry
          remoteCached?: RemoteFileEntry

          localFresh?: LocalFileEntryFresh
          localCached?: LocalFileEntry
        }
      > = {}

      for await (const remoteFresh of getRemote(syncEntry.remoteFileId, [])) {
        Object.assign((map[`${remoteFresh.path}`] ??= {}), { remoteFresh })
      }

      for await (const localFresh of getLocal(syncEntry.localFileHandle, [])) {
        Object.assign((map[`${localFresh.path}`] ??= {}), { localFresh })
      }

      for await (const remoteCached of await database.remoteFiles.toArray()) {
        Object.assign((map[`${remoteCached.path}`] ??= {}), { remoteCached })
      }

      for await (const localCached of await database.localFiles.toArray()) {
        Object.assign((map[`${localCached.path}`] ??= {}), { localCached })
      }

      for (const path in map) {
        const {
          [path]: { localCached: lc, localFresh: lf, remoteCached: rc, remoteFresh: rf }
        } = map

        if (context.cancelled) {
          return
        }
        const ls = lc?.lastModified == lf?.lastModified
        const rs = rc?.fileSnapshotId == rf?.fileSnapshotId

        if (lc && lf && rc && rf) {
          if (rs && ls) {
            continue
          } else if (rs) {
          } else if (ls) {
            try {
              const fileContent = await server.getMainFileContent(rf.fileId)
              const fileSnapshot = (
                await server.getFileSnapshots(rf.fileId, void 0, rf.fileSnapshotId, 0, 1)
              )[0]

              const fileEntry = await new Promise<File>((resolve, reject) =>
                lf.entry.file(resolve, reject)
              )
              const parent = await new Promise((resolve: DirectoryEntryCallback, reject) =>
                lf.entry.getParent(resolve, reject)
              )

              const localFileHandle = await new Promise((resolve: FileWriterCallback, reject) =>
                parent.getFile(
                  fileEntry.name,
                  { create: false, exclusive: false },
                  (entry) => entry.createWriter(resolve, reject),
                  reject
                )
              )

              const remoteFileHandle = await server.openStream(
                rf.fileId,
                fileContent.id,
                fileSnapshot.id
              )
              const size = fileSnapshot.size

              const bufferSize = 1024 * 256
              for (let offset = 0; offset < size; offset += bufferSize) {
                const buffer = new Blob([await server.readStream(remoteFileHandle, bufferSize)])

                localFileHandle.position = offset
                localFileHandle.write(buffer)
              }

              localFileHandle.truncate(size)
            } catch {
              //
            }
          } else {
          }
        } else if (lc && lf && rc && !rf) {
        } else if (lc && lf && !rc && rf) {
        } else if (lc && lf && !rc && !rf) {
        } else if (lc && !lf && rc && rf) {
        } else if (lc && !lf && rc && !rf) {
        } else if (lc && !lf && !rc && rf) {
        } else if (lc && !lf && !rc && !rf) {
        } else if (!lc && lf && rc && rf) {
        } else if (!lc && lf && rc && !rf) {
        } else if (!lc && lf && !rc && rf) {
        } else if (!lc && lf && !rc && !rf) {
        } else if (!lc && !lf && rc && rf) {
        } else if (!lc && !lf && rc && !rf) {
        } else if (!lc && !lf && !rc && rf) {
        } else if (!lc && !lf && !rc && !rf) {
        }
      }
    }

    try {
      const jobContext: JobContext = { cancelled: false }

      await run(jobContext)

      syncJobs[syncEntry.id] = jobContext
      // syncJobs.set(syncEntry.id, jobContext)
    } catch {
      delete syncJobs[syncEntry.id]
    }
  }

  void initSync()

  return { context, database }
}

interface JobContext {
  cancelled: boolean
}

export interface RemoteFileEntry {
  fileId: string
  fileSnapshotId?: string

  lastModified: Date
  path: string[]
}

export interface LocalFileEntry {
  lastModified: Date
  path: string[]
}

export interface LocalFileEntryFresh extends LocalFileEntry {
  entry: FileSystemFileEntry
}

export interface SyncEntry {
  id: number

  remoteFileId: string
  localFileHandle: FileSystemDirectoryEntry
}

export interface RemoteCachedSyncFile {
  cached?: RemoteFileEntry
  fresh?: RemoteFileEntry
  file: FileResource
  lastModified: Date
  staleCache: boolean
}

export interface LocalCachedSyncFile {
  cached?: LocalFileEntry
  fresh?: LocalFileEntry
  handle: FileSystemFileEntry
  lastModified: Date
  isStale: boolean
}
