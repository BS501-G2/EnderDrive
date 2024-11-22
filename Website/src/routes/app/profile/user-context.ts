import { getContext, setContext } from 'svelte'

const name = Symbol('User Context')

export function useUserContext() {
  return getContext<UserContext>(name)
}

export type UserContext = ReturnType<typeof createUserContext>['context']

export function createUserContext() {
  const context = setContext(name, {})

  return { context }
}
