import type { IconOptions } from '$lib/client/ui/icon.svelte'
import { getContext, setContext, type Snippet } from 'svelte'
import { writable, type Writable } from 'svelte/store'

const name = Symbol('User Context')

export function useUserContext() {
  return getContext<UserContext>(name)
}

export type UserContext = ReturnType<typeof createUserContext>['context']

export function createUserContext() {
  const tabs: Writable<{ id: number; label: string; icon: IconOptions; snippet: Snippet }[]> =
    writable([])

  const currentTabIndex: Writable<number> = writable(0)

  const context = setContext(name, {
    pushTab: (label: string, icon: IconOptions, snippet: Snippet) => {
      const id = Math.random()

      tabs.update((tabs) => [...tabs, { id, label, icon, snippet }])

      return () => tabs.update((tabs) => tabs.filter((tab) => tab.id !== id))
    }
  })

  return { context, tabs, currentTabIndex }
}
