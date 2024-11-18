import { getContext, setContext, type Snippet } from 'svelte'
import { persisted } from 'svelte-persisted-store'
import { derived, get, writable, type Readable, type Writable } from 'svelte/store'
import type {
  FileResource,
  FileAccessResource,
  FileStarResource,
  FileType,
  VirusReportResource
} from '../client'
import type { IconOptions } from '../ui/icon.svelte'
import type { FileBrowserListContext } from './file-browser-list'

const contextName = 'File Browser Context'

export enum FileBrowserResolveType {
  File,
  Shared,
  Starred,
  Trash
}

export type FileBrowserResolve =
  | [type: FileBrowserResolveType.File, fileId: string | null]
  | [type: FileBrowserResolveType.Shared]
  | [type: FileBrowserResolveType.Trash]
  | [type: FileBrowserResolveType.Starred]

export interface FileBrowserOptions {
  resolve: Readable<FileBrowserResolve>

  customContext?: ReturnType<typeof createFileBrowserContext>

  selectMode?: {
    maxSelectionCount: number

    allowedFileMimeTypes: (RegExp | string)[]
  } | null

  onFileId?: (
    event: MouseEvent & {
      currentTarget: EventTarget & HTMLButtonElement
    },
    fileId: string | null
  ) => Promise<void>
}

export type FileBrowserContext = ReturnType<typeof createFileBrowserContext>['context']

export function useFileBrowserContext() {
  return getContext<FileBrowserContext>(contextName)
}

export interface FileBrowserAction {
  id: number
  snippet: Snippet
  icon: IconOptions
  onclick: (
    event: MouseEvent & {
      currentTarget: EventTarget & HTMLButtonElement
    }
  ) => void
  label: string

  type: 'left-main' | 'left' | 'right-main' | 'right'
}

export function createFileBrowserContext(
  onFileId?: FileBrowserOptions['onFileId'],
  selectMode?: FileBrowserOptions['selectMode']
) {
  const showDetails = persisted('file-browser-config-show-details', false)
  const isLoading = writable(false)
  const top = writable<
    {
      id: number
      snippet: Snippet
    }[]
  >([])
  const middle = writable<
    {
      id: number
      snippet: Snippet
    }[]
  >([])
  const bottom = writable<
    {
      id: number
      snippet: Snippet
    }[]
  >([])
  const actions = writable<FileBrowserAction[]>([])
  const current = writable<CurrentFile>({
    type: 'loading'
  })
  const refresh = writable<(() => void) | null>(null)

  const fileListContext: Writable<FileBrowserListContext | null> = writable(null)

  const context = setContext(contextName, {
    showDetails,

    pushTop: (snippet: Snippet) => {
      const id = Math.random()

      top.update((value) => [
        ...value,
        {
          id,
          snippet
        }
      ])

      return () => top.update((value) => value.filter((value) => value.id !== id))
    },

    pushMiddle: (snippet: Snippet) => {
      const id = Math.random()

      middle.update((value) => [
        ...value,
        {
          id,
          snippet
        }
      ])

      return () => middle.update((value) => value.filter((value) => value.id !== id))
    },

    pushBottom: (snippet: Snippet) => {
      const id = Math.random()

      bottom.update((value) => [
        ...value,
        {
          id,
          snippet
        }
      ])

      return () => bottom.update((value) => value.filter((value) => value.id !== id))
    },

    pushAction: (
      snippet: Snippet,
      type: FileBrowserAction['type'],
      icon: IconOptions,
      label: string,
      onclick: (
        event: MouseEvent & {
          currentTarget: EventTarget & HTMLButtonElement
        }
      ) => void
    ) => {
      const id = Math.random()

      actions.update((actions) => [
        ...actions,
        {
          id,
          snippet,
          type,
          icon,
          label,
          onclick
        }
      ])

      return () => actions.update((actions) => actions.filter((action) => action.id !== id))
    },

    current: derived(current, (value) => value),

    onFileId,
    selectMode,

    fileListContext: derived(fileListContext, (value) => value),

    setFileListContext: (context: FileBrowserListContext) => {
      fileListContext.set(context)

      return () =>
        fileListContext.update((value) => {
          if (value != context) {
            return value
          }

          return null
        })
    },

    refresh: () => get(refresh)?.(),

    pushRefresh: (callback: () => void) => {
      if (get(refresh) != null) {
        throw new Error('Refresh callback is already registered')
      }

      refresh.set(callback)

      return () =>
        refresh.update((value) => {
          if (value != callback) {
            return value
          }

          return null
        })
    }
  })

  return {
    showDetails,
    top,
    middle,
    bottom,
    context,
    isLoading,
    actions,
    current,
    fileListContext
  }
}

export type FileEntry = {
  file: FileResource
} & (
  | {
      type: 'folder'
      file: FileResource & {
        type: FileType.Folder
      }
    }
  | {
      type: 'file'
      file: FileResource & {
        type: FileType.File
      }
    }
  | {
      type: 'shared'
      fileAccess: FileAccessResource
    }
  | {
      type: 'starred'
      fileStar: FileStarResource
    }
  | {
      type: 'trash'
    }
)

export type CurrentFile =
  | ((
      | {
          type: 'file'
          mime: string
        }
      | {
          type: 'folder'
          files: FileEntry[]
        }
    ) & {
      path: FileResource[]
      file: FileResource
    })
  | {
      type: 'shared' | 'starred' | 'trash'
      files: FileEntry[]
    }
  | {
      type: 'loading'
    }
  | {
      type: 'error'
      error: Error
    }

export interface FileProperties {
  file: FileResource
  viruses: VirusReportResource | null

  created: Date
  modified: Date
  mime: string
  size: number
}
