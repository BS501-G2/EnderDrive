import { getContext, setContext, type Snippet } from 'svelte'
import { writable, type Writable } from 'svelte/store'
import type { IconOptions } from '../ui/icon.svelte'

const contextName = 'FileBrowserPropertiesContext'

export type FileBrowserPropertiesContext = ReturnType<
  typeof createFileBrowserPropertiesContext
>['context']

export function useFileBrowserPropertiesContext() {
  return getContext<FileBrowserPropertiesContext>(contextName)
}

export function createFileBrowserPropertiesContext() {
  const tabs: Writable<
    {
      id: number
      name: string
      icon: IconOptions
      content: Snippet
    }[]
  > = writable([])

  const currentTab: Writable<number> = writable(0)

  const context = setContext(contextName, {
    pushTab(name: string, icon: IconOptions, content: Snippet) {
      const id = Math.random()

      tabs.update((value) => [
        ...value,
        {
          id,
          name,
          icon,
          content
        }
      ])

      return () => tabs.update((value) => value.filter((tab) => tab.id !== id))
    }
  })

  return { context, tabs, currentTab }
}
