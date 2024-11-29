import * as SocketIO from 'socket.io-client'

import { derived, get, writable, type Readable } from 'svelte/store'

export type ConnectionPacket = {
  id: string
} & (
  | {
      type: 'request'
      name: string
      data: unknown
    }
  | {
      type: 'response'
      data: unknown
    }
  | {
      type: 'error'
      message: string
      stack?: string
    }
)

export interface Connection {
  state: Readable<ConnectionState>

  sendRequest: (name: string, data: unknown) => Promise<unknown>
}

export type ConnectionState = [type: 'connecting'] | [type: 'connnected'] | [type: 'disconnected']

export function createConnection(
  handleRequest: (name: string, data: unknown) => Promise<unknown>
): Connection {
  const url = `ws${window.location.protocol === 'https:' ? 's' : ''}://${window.location.host}/`
  const socket = SocketIO.connect(url)
  const state = writable<ConnectionState>(['connecting'])

  const send = (data: ConnectionPacket) => {
    if (get(state)[0] === 'connnected') {
      socket.emit('', data)

      return
    }

    throw new Error('Failed to send.')
  }

  const pendingRequests: Record<
    string,
    {
      id: string
      resolve: (data: unknown) => void
      reject: (error: Error) => void
    }
  > = {}

  socket.on('', (packet: ConnectionPacket) => {
    const { id } = packet

    if (packet.type === 'request') {
      const { name, data } = packet

      handleRequest(name, data)
        .then((data) => send({ type: 'response', id, data }))
        .catch((error: Error) =>
          send({ type: 'error', id, message: error.message, stack: error.stack })
        )
    } else if (id in pendingRequests) {
      const {
        [id]: { resolve, reject }
      } = pendingRequests

      delete pendingRequests[id]

      if (packet.type === 'error') {
        reject(
          Object.assign(new Error(packet.message), {
            stack: packet.stack
          })
        )
      } else if (packet.type === 'response') {
        resolve(packet.data)
      }
    }
  })

  socket.on('connect', () => {})

  socket.on('disconnect', () => {
    for (const id in pendingRequests) {
      const {
        [id]: { reject }
      } = pendingRequests
      delete pendingRequests[id]

      reject(new Error('Disconnected'))
    }
  })

  socket.on('connect_error', console.error)

  socket.io.on('reconnect_attempt', () => {})

  const stateReadonly = derived(state, (state) => structuredClone(state))
  const sendRequest = (name: string, data: unknown): Promise<unknown> => {
    const id = Math.round(Math.random() * 1000000).toString(16)

    return new Promise<unknown>((resolve: (data: unknown) => void, reject) => {
      pendingRequests[id] = { id, resolve, reject }

      send({ id, name, data, type: 'request' })
    })
  }

  return {
    get state() {
      return stateReadonly
    },

    get sendRequest() {
      return sendRequest
    }
  }
}
