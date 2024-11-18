import { getContext, setContext } from 'svelte'

export const contextName = Symbol('Notification Context')

export function useNotificationContext() {
  return getContext<NotificationContext>(contextName)
}

export type NotificationContext = ReturnType<typeof createNotificationContext>['context']

export function createNotificationContext(reload: () => void) {
  const context = setContext(contextName, {
    reload
  })

  return {
    context
  }
}
