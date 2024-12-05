import type { NotificationResource } from '$lib/client/resource'
import { getContext, setContext } from 'svelte'
import { writable, type Writable } from 'svelte/store'

const name = Symbol('Notification Context')

export type NotificationContext = ReturnType<typeof createNotificationContext>['context']

export function getNotificationContext() {
  return getContext<NotificationContext>(name)
}

export function createNotificationContext() {
  const toasts: Writable<{ id: number; notification: NotificationResource; timeout: number }[]> =
    writable([])
  const unread: Writable<Promise<number>> = writable(Promise.resolve(0))
  const desktopButtonElement: Writable<HTMLDivElement | null> = writable(null as never)
  const notificationPage: Writable<{ focusId: string | null } | null> = writable(null)

  const context = setContext(name, {
    pushToast(notification: NotificationResource) {
      const id = Math.random()

      toasts.update((toasts) => [...toasts, { id, notification, timeout: 100000 }])

      return () => toasts.update((toasts) => toasts.filter((toast) => toast.id !== id))
    },
    unread,notificationPage,
    desktopButtonElement
  })

  return { context, toasts, unread, notificationPage, desktopButtonElement }
}
