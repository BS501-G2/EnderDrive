import { getContext, setContext, type Snippet } from 'svelte'
import { derived, writable, type Readable, type Writable } from 'svelte/store'
import type { IconOptions } from '../ui/icon.svelte'
import type { NotificationContext } from '../../../routes/app/notification-context'

const contextName = `${Date.now()}`

export function useDashboardContext(context?: DashboardContext) {
  return context != null
    ? setContext(contextName, context)
    : getContext<DashboardContext>(contextName)
}

export type DashboardContext = ReturnType<typeof createDashboardContext>['context']
export interface BackgroundTask {
  id: number

  state: Writable<BackgroundTaskState>

  clear: () => void
}

export interface BackgroundTaskState {
  status: [type: 'pending'] | [type: 'done'] | [type: 'error', error: Error]
  progress: [current: number, total: number] | null
  message: string | null
  title: string
  footerLeft: string | null
  footerRight: string | null
}

export interface BackgroundTaskContext {
  setTitle: (title: string) => void
  setMessage: (message: string) => void
  setProgress: (progress: [current: number, total: number] | null) => void
  setFooterLeft: (footerLeft: string | null) => void
  setFooterRight: (footerRight: string | null) => void
}

export function createDashboardContext(notification: Readable<NotificationContext>) {
  const mobileAppButtons: Writable<
    {
      id: number
      snippet: Snippet<[ondismiss: () => void]>
      onclick: (
        event: MouseEvent & {
          currentTarget: EventTarget & HTMLButtonElement
        }
      ) => void
      show: boolean
      icon: IconOptions
    }[]
  > = writable([])
  const mobileTopLeft: Writable<{ id: number; snippet: Snippet }[]> = writable([])
  const mobileTopRight: Writable<{ id: number; snippet: Snippet }[]> = writable([])
  const mobileBottom: Writable<{ id: number; snippet: Snippet }[]> = writable([])

  const desktopSide: Writable<{ id: number; snippet: Snippet }[]> = writable([])
  const desktopTopLeft: Writable<{ id: number; snippet: Snippet }[]> = writable([])

  const desktopTopMiddle: Writable<{ id: number; snippet: Snippet }[]> = writable([])
  const desktopTopRight: Writable<{ id: number; snippet: Snippet }[]> = writable([])
  const desktopBottom: Writable<{ id: number; snippet: Snippet; arrangement: 'left' | 'right' }[]> =
    writable([])
  const backgroundTasks: Writable<BackgroundTask[]> = writable([])

  const context = setContext(contextName, {
    notification: derived(notification, (value) => value),

    pushMobileAppButton: (
      snippet: Snippet<[ondismiss: () => void]>,
      show: boolean,
      icon: IconOptions,
      onclick: (
        event: MouseEvent & {
          currentTarget: EventTarget & HTMLButtonElement
        }
      ) => void
    ) => {
      const id = Math.random()
      mobileAppButtons.update((value) => [
        ...value,
        {
          id,
          snippet,
          show,
          icon,
          onclick
        }
      ])

      return () => mobileAppButtons.update((value) => value.filter((value) => value.id !== id))
    },

    pushMobileTopLeft: (snippet: Snippet) => {
      const id = Math.random()
      mobileTopLeft.update((value) => [
        ...value,
        {
          id,
          snippet
        }
      ])

      return () => mobileTopLeft.update((value) => value.filter((value) => value.id !== id))
    },

    pushMobileTopRight: (snippet: Snippet) => {
      const id = Math.random()
      mobileTopRight.update((value) => [
        ...value,
        {
          id,
          snippet
        }
      ])

      return () => mobileTopRight.update((value) => value.filter((value) => value.id !== id))
    },

    pushMobileBottom: (snippet: Snippet) => {
      const id = Math.random()
      mobileBottom.update((value) => [
        ...value,
        {
          id,
          snippet
        }
      ])

      return () => mobileBottom.update((value) => value.filter((value) => value.id !== id))
    },

    pushDesktopSide: (snippet: Snippet) => {
      const id = Math.random()
      desktopSide.update((value) => [
        ...value,
        {
          id,
          snippet
        }
      ])

      return () => desktopSide.update((value) => value.filter((value) => value.id !== id))
    },

    pushDesktopTopLeft: (snippet: Snippet) => {
      const id = Math.random()
      desktopTopLeft.update((value) => [
        ...value,
        {
          id,
          snippet
        }
      ])

      return () => desktopTopLeft.update((value) => value.filter((value) => value.id !== id))
    },

    pushDesktopTopMiddle: (snippet: Snippet) => {
      const id = Math.random()
      desktopTopMiddle.update((value) => [
        ...value,
        {
          id,
          snippet
        }
      ])

      return () => desktopTopMiddle.update((value) => value.filter((value) => value.id !== id))
    },

    pushDesktopTopRight: (snippet: Snippet) => {
      const id = Math.random()
      desktopTopRight.update((value) => [
        ...value,
        {
          id,
          snippet
        }
      ])

      return () => desktopTopRight.update((value) => value.filter((value) => value.id !== id))
    },

    pushDesktopBottom: (snippet: Snippet, arrangement: 'left' | 'right') => {
      const id = Math.random()

      desktopBottom.update((value) => [...value, { id, snippet, arrangement }])

      return () => desktopBottom.update((value) => value.filter((value) => value.id !== id))
    },

    executeBackgroundTask: async <T>(
      title: string,
      run: (context: BackgroundTaskContext) => Promise<T>
    ): Promise<T> => {
      const id = Math.random()
      const state = writable<BackgroundTaskState>({
        status: ['pending'],
        progress: null,
        message: null,
        title,
        footerLeft: null,
        footerRight: null
      })

      const context: BackgroundTaskContext = {
        setMessage: (message) => {
          state.update((value) => {
            value.message = message

            return value
          })
        },

        setProgress: (progress) => {
          state.update((value) => {
            value.progress = progress

            return value
          })
        },

        setTitle: (title) => {
          state.update((value) => {
            value.title = title

            return value
          })
        },

        setFooterLeft: (footerLeft) => {
          state.update((value) => {
            value.footerLeft = footerLeft

            return value
          })
        },

        setFooterRight: (footerRight) => {
          state.update((value) => {
            value.footerRight = footerRight

            return value
          })
        }
      }

      backgroundTasks.update((value) => {
        value.push({
          id,
          state,
          clear: () => backgroundTasks.update((value) => value.filter((value) => value.id !== id))
        })

        return value
      })

      try {
        const result = await run(context)
        state.update((value) => {
          value.status = ['done']
          value.title = title

          value.progress = null
          value.message = null
          value.footerLeft = null
          value.footerRight = null

          return value
        })

        return result
      } catch (error: unknown) {
        state.update((value) => {
          value.status = ['error', error as never]
          value.title = title

          value.progress = null
          value.message = null
          value.footerLeft = null
          value.footerRight = null

          return value
        })

        throw error
      }
    }
  })

  return {
    mobileAppButtons,
    mobileTopLeft,
    mobileTopRight,
    mobileBottom,
    desktopSide,
    desktopTopLeft,
    desktopTopMiddle,
    desktopTopRight,
    backgroundTasks,
    desktopBottom,
    context
  }
}
