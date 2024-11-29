/* eslint-disable @typescript-eslint/no-unused-vars */
/* eslint-disable @typescript-eslint/no-explicit-any */
import { Buffer } from 'buffer'
import { getContext, setContext } from 'svelte'
import { derived, get, writable, type Readable, type Writable } from 'svelte/store'

import { persisted } from 'svelte-persisted-store'
import type { NotificationContext } from './contexts/notification'
import { goto } from '$app/navigation'
import { createConnection } from './connection'

const name = Symbol('Client Context')

export type ClientContext = ReturnType<typeof createClientContext>['context']

export function createClientContext() {
  const requestHandlers: Partial<FunctionMap<ClientSideFunctions>> = {}

  const connection = createConnection((name, data) => requestHandlers[name]!(data as never))

  const { sendRequest } = connection
  const context = setContext(name, {
    server: new Proxy<FunctionMap<ServerSideFunctions>>({} as never, {
      get:
        (...[, name]) =>
        (data: unknown) =>
          sendRequest(`${name as never}`, data)
    }),
    client: {
      setHandler: <T extends keyof FunctionMap<ClientSideFunctions>>(
        name: T,
        handler: (
          request: Parameters<FunctionMap<ClientSideFunctions>[T]>[0]
        ) => ReturnType<FunctionMap<ClientSideFunctions>[T]>
      ) => {
        if (name in requestHandlers) {
          throw new Error('Handler is already set')
        }

        requestHandlers[name] = handler

        return () => {
          delete requestHandlers[name]
        }
      }
    }
  })

  return { context }
}

export type RemoteFunction<S extends Record<string, any>, R extends Record<string, any>> = [
  request: S,
  response: R
]

export interface Authentication {
  userId: string
  token: string
}

export interface ServerSideFunctions
  extends Record<string, RemoteFunction<Record<string, any>, Record<string, any>>> {
  getSetupRequirements: [object, { adminSetupRequired: boolean }]
  authenticatePassword: [{ username: string; password: string }, { authentication: Authentication }]

  // getNotifications: [{ afterId: number }, { notifications }]
}

export interface ClientSideFunctions
  extends Record<string, RemoteFunction<Record<string, any>, Record<string, any>>> {
  refreshNotification: [object, object]
}

export type FunctionMap<T extends ServerSideFunctions | ClientSideFunctions> = {
  [key in keyof T]: (request: T[key][0]) => Promise<T[key][1]>
}
