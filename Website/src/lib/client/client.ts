/* eslint-disable @typescript-eslint/no-explicit-any */

import { getContext, setContext } from 'svelte'
import { derived, get, writable, type Writable } from 'svelte/store'

import { persisted } from 'svelte-persisted-store'
import { type ConnectionState, createConnection } from './connection'
import {
  responseTranslators as serverSideResponseTranslators,
  type ServerSideFunctions
} from './server-functions'
import { responseTranslators, type ClientSideFunctions } from './client-functions'

const name = Symbol('Client Context')

export type ClientContext = ReturnType<typeof createClientContext>['context']

export type RemoteFunction<S = object, R = object, RS = S, RR = R> = [
  request: S,
  response: R,
  rawRequest: RS extends void ? S : RS,
  rawResponse: RR extends void ? R : RR
]

export interface Authentication {
  UserId: string
  Token: string
}

export type FunctionMap<T extends ServerSideFunctions | ClientSideFunctions> = {
  [key in keyof T]: (request: T[key][0]) => Promise<T[key][1]>
}

export type TranslatorMap<T extends ServerSideFunctions | ClientSideFunctions> = {
  [key in keyof T]: [
    request: (data: T[key][2]) => T[key][0],
    response: (data: T[key][3]) => T[key][1]
  ]
}

export type CustomFunctionMap<T extends ServerSideFunctions | ClientSideFunctions> = {
  [key in keyof FunctionMap<T>]: (
    data: T[key][0],
    request: FunctionMap<T>[key]
  ) => ReturnType<FunctionMap<T>[key]>
}

export interface PaginationOptions {
  Count?: number
  Offset?: number
}

export function createClientContext() {
  const requestHandlers: Partial<FunctionMap<ClientSideFunctions>> = {}

  const connection = createConnection((name, data) =>
    requestHandlers[name]!(responseTranslators[name][0](data) as never)
  )

  const authentication: Writable<Authentication | null> = persisted('stored-token', null as never)

  const clientState = writable<ConnectionState>(['connecting'])

  connection.state.subscribe(async (state) => {
    if (state[0] === 'connecting') {
      clientState.set(state)
    } else if (state[0] === 'connnected') {
      const auth = get(authentication)

      if (auth != null) {
        try {
          await sendRequest('AuthenticateToken', auth)
        } catch {
          //
        }
      }

      let preRequest: (() => Promise<void>) | undefined
      while ((preRequest = preRequests.shift())) {
        try {
          await preRequest()
        } catch (error: any) {
          console.error(error)
        }
      }

      clientState.set(state)
    } else if (state[0] === 'disconnected') {
      clientState.set(state)
    }
  })

  async function sendRequest<T extends keyof FunctionMap<ServerSideFunctions>>(
    name: T,
    data: ServerSideFunctions[T][0]
  ): Promise<ServerSideFunctions[T][1]> {
    const a = (data: ServerSideFunctions[T][0]) =>
      connection.sendRequest(name as string, data).then(serverSideResponseTranslators[name][1])

    const customRequest = customServerSideFunctions[name]

    try {
      const request: FunctionMap<ServerSideFunctions>[T] =
        customRequest != null ? (data) => customRequest(data, a) : a

      return request(data)
    } catch (error: any) {
      throw new Error(error.message, { cause: error })
    }
  }

  const preRequests: (() => Promise<void>)[] = []

  const customServerSideFunctions: Partial<CustomFunctionMap<ServerSideFunctions>> = {
    AuthenticateGoogle: async (data, request) => {
      const result = await request(data)

      authentication.set(result)
      return result
    },

    AuthenticatePassword: async (data, request) => {
      const result = await request(data)

      authentication.set(result)
      return result
    },

    AuthenticateToken: async (data, request) => {
      try {
        const renewedToken = await request(data)

        authentication.update((authentication) => {
          if (authentication == null) {
            return null
          }

          return { UserId: authentication.UserId, Token: renewedToken ?? authentication.Token }
        })

        return renewedToken
      } catch (error: unknown) {
        authentication.set(null)
        throw error
      }
    },

    Deauthenticate: async (data, request) => {
      const result = await request(data)

      authentication.set(null)

      return result
    }
  }

  const context = setContext(name, {
    server: new Proxy<FunctionMap<ServerSideFunctions>>({} as never, {
      get: <T extends keyof FunctionMap<ServerSideFunctions>>(
        ...[, name]: [never, T | symbol]
      ): FunctionMap<ServerSideFunctions>[T] => {
        if (typeof name === 'symbol') {
          return void 0 as never
        }

        return get(clientState)[0] !== 'connnected'
          ? (data) =>
              new Promise((resolve, reject) =>
                preRequests.push(() => sendRequest(name, data).then(resolve, reject))
              )
          : (data) => sendRequest(name, data)
      }
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
    },

    clientState: derived(clientState, (clientState) => clientState),

    authentication
  })

  requestHandlers['IsHandlerAvailable'] = ({ Name }) =>
    Promise.resolve({
      IsAvailable: Name in requestHandlers
    })

  return { context, authentication }
}

export function useClientContext() {
  return getContext<ClientContext>(name)
}
