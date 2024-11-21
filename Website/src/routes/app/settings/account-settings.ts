import { getContext, setContext, type Snippet } from 'svelte'
import { writable } from 'svelte/store'
import type { IconOptions } from '$lib/client/ui/icon.svelte'

const name = Symbol('Settings Context')

export type AccountSettingsContext = ReturnType<typeof createAccountSettingsContext>['context']

export function useAccountSettingsContext() {
  return getContext<AccountSettingsContext>(name)
}

export function createAccountSettingsContext() {
  const tabs = writable<{ id: number; name: string; icon: IconOptions; snippet: Snippet }[]>([])
  const currentTab = writable<number>(0)

  const context = setContext(name, {
    pushTab: (name: string, icon: IconOptions, snippet: Snippet) => {
      const id = Math.random()

      tabs.update((tabs) => [...tabs, { id, name, icon, snippet }])

      return () => tabs.update((tabs) => tabs.filter((tab) => tab.id !== id))
    }
  })

  return { context, tabs, currentTab }
}
