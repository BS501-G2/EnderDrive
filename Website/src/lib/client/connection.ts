import * as SocketIO from 'socket.io-client'

import { derived, get, writable, type Readable } from 'svelte/store'

export type ConnectionPacket = {
  id: number
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

export type ConnectionState =
  | [type: 'connecting']
  | [type: 'connnected']
  | [type: 'disconnected', message: string, retry: () => void]

export function createConnection(
  handleRequest: (name: string, data: unknown) => Promise<unknown>
): Connection {
  const url = `ws${window.location.protocol === 'https:' ? 's' : ''}://${window.location.host}/`
  const socket = SocketIO.connect(url)
  const state = writable<ConnectionState>(['connecting'])

  const send = (data: ConnectionPacket) => {
    if (get(state)[0] === 'connnected') {
      console.debug('->', data)
      socket.emit('', data)

      return
    }

    throw new Error('Failed to send.')
  }

  const pendingRequests: Record<
    string,
    {
      id: number
      resolve: (data: unknown) => void
      reject: (error: Error) => void
    }
  > = {}

  socket.on('', (packet: ConnectionPacket) => {
    const { id } = packet
    console.debug('<-', packet)

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

  socket.on('connect', async () => {
    state.set(['connnected'])
  })

  socket.on('disconnect', () => {
    for (const id in pendingRequests) {
      const {
        [id]: { reject }
      } = pendingRequests
      delete pendingRequests[id]

      reject(new Error('Disconnected'))
    }

    console.log('disconnect2')

    state.set(['disconnected', 'Connection closed.', () => socket.connect()])
  })

  socket.on('connect_error', () => {
    state.set(['disconnected', 'Failed to connect.', () => socket.connect()])
  })

  socket.io.on('reconnect_attempt', () => {
    state.set(['connecting'])
  })

  const stateReadonly = derived(state, (state) => state)
  const sendRequest = (name: string, data: unknown): Promise<unknown> => {
    const id = Math.round(Math.random() * 1000000)

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
