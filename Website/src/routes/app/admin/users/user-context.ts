import type { UserRole } from '$lib/client/resource'
import { getContext, setContext } from 'svelte'
import { writable } from 'svelte/store'

const name = Symbol('User Context')

export type UserContext = ReturnType<typeof createUserContext>['context']

export function useUserContext() {
  return getContext<UserContext>(name)
}

export function createUserContext() {
  const searchString = writable('')
  const excludeRole = writable<UserRole[]>([])
  const includeRole = writable<UserRole[]>([])

  const context = setContext(name, { searchString, excludeRole, includeRole })

  return { context, searchString, excludeRole, includeRole }
}
