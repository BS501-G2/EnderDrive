import { useDashboardContext } from '$lib/client/contexts/dashboard'
import { getContext, setContext } from 'svelte'
import { get } from 'svelte/store'

const name = Symbol('Notification Context')

export type NotificationContext = ReturnType<typeof createNotificationContext>['context']

export function getNotificationContext() {
  const dashboard = useDashboardContext()
  if (dashboard != null) {
    const context = get(dashboard.notification)

    if (context != null) {
      return context
    }
  }

  return getContext<NotificationContext>(name)
}

export function createNotificationContext() {
  const context = setContext(name, {})

  return { context }
}
