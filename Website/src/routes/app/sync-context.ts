import { setContext } from 'svelte'

const name = Symbol('Sync Context')

export function createSyncContext() {
  const context = setContext(name, {})

  return { context }
}
