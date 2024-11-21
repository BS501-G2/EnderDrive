import { getContext, setContext, type Snippet } from 'svelte'
import { writable } from 'svelte/store'

const name = Symbol('Select Context')

export function useSelectContext<T>() {
  return getContext<SelectContext<T>>(name)
}

export type SelectContext<T> = ReturnType<typeof createSelectContext<T>>['context']

export function createSelectContext<T>() {
  const options = writable<{ id: string; label: string; value: T; snippet: Snippet }[]>([])

  const context = setContext(name, {
    pushOption: (label: string, value: T, snippet: Snippet) => {
      const id = Math.random().toString()

      options.update((options) => [...options, { id, value, label, snippet }])

      return () => options.update((options) => options.filter((option) => option.id !== id))
    }
  })

  return { context, options }
}
