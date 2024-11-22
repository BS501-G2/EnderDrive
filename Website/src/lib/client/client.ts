/* eslint-disable @typescript-eslint/no-unused-vars */
/* eslint-disable @typescript-eslint/no-explicit-any */
import { Buffer } from 'buffer'
import { getContext, setContext } from 'svelte'
import { derived, get, writable, type Readable, type Writable } from 'svelte/store'
import * as SocketIO from 'socket.io-client'

import { persisted } from 'svelte-persisted-store'
import type { NotificationContext } from './contexts/notification'

const clientContextName = 'Client Context'

enum PacketType {
  Request = 0,
  RequestCancel = 1,
  Response = 2,
  ResponseCancel = 3,
  ResponseError = 4,
  ResponseInternalError = 5
}

enum InternalErrorReason {
  DuplicateId = 0
}

export type Packet<C extends ClientSideRequestCode | ServerSideRequestCode | ResponseCode> =
  | {
      type: PacketType.Request
      id: number
      data: Payload<C>
    }
  | {
      type: PacketType.RequestCancel
      id: number
    }
  | {
      type: PacketType.Response
      id: number
      data: Payload<C>
    }
  | {
      type: PacketType.ResponseCancel
      id: number
    }
  | {
      type: PacketType.ResponseError
      id: number
      error: string
    }
  | {
      type: PacketType.ResponseInternalError
      id: number
      reason: number
    }

export enum ClientState {
  Connecting,
  Connected,
  Failed
}

export interface ClientPacket<
  T extends ServerSideRequestCode | ClientSideRequestCode | ResponseCode
> {
  Code: T
  Data: any
}

export interface Payload<T extends ClientSideRequestCode | ServerSideRequestCode | ResponseCode> {
  Code: T
  Data: object
}

export type ClientContext = ReturnType<typeof createClientContext>['context']

const requestSymbol: unique symbol = Symbol('Request')
const requestFunctionsSymbol: unique symbol = Symbol('Request Functions')

type ClientStateWritable =
  | [state: ClientState.Connecting]
  | [state: ClientState.Connected]
  | [state: ClientState.Failed, error: Error, retry: () => void]

export function createClientContext() {
  const state = writable<ClientStateWritable>([ClientState.Connecting])

  const incomingResponses: Map<
    number,
    {
      resolve: (data: Payload<any>) => void
      reject: (reason: Error) => void
    }
  > = new Map()

  const proxiedSate = writable<ClientStateWritable>([ClientState.Connecting])

  const incomingRequests: Map<number, () => void> = new Map()

  const requestHandlers: Map<
    number,
    (data: any, isCancelled: () => boolean) => Promise<Payload<ResponseCode>>
  > = new Map()

  let nextRequestId: number = 0
  const url = `ws${window.location.protocol === 'https:' ? 's' : ''}://${window.location.host}/`

  const socket = SocketIO.connect(url, {
    reconnection: false,
    autoConnect: false
  })

  function send<T extends ClientSideRequestCode | ServerSideRequestCode | ResponseCode>(
    packet: Packet<T>
  ) {
    console.debug(
      '->',
      JSON.stringify(packet) /** new Error().stack?.split('\n').slice(1).join('\n') */
    )
    socket.emit('message', packet)
  }

  const connect = () => {
    socket.on('message', (packet: Packet<any>) => {
      console.debug(
        '<-',
        JSON.stringify(packet) /**new Error().stack?.split('\n').slice(1).join('\n')u */
      )
      switch (packet.type) {
        case PacketType.Request: {
          if (incomingRequests.has(packet.id)) {
            send({
              type: PacketType.ResponseInternalError,
              id: packet.id,
              reason: InternalErrorReason.DuplicateId
            })
            break
          }

          let cancelled: boolean = false
          incomingRequests.set(packet.id, () => {
            cancelled = true
          })

          void (async () => {
            try {
              try {
                const handler = requestHandlers.get(packet.data.Code)
                const response: Payload<ResponseCode> =
                  handler != null
                    ? await handler(packet.data.Data, () => cancelled)
                    : {
                        Code: ResponseCode.NoHandlerFound,
                        Data: {}
                      }

                send({
                  type: PacketType.Response,
                  id: packet.id,
                  data: response
                })
              } catch (error: any) {
                send({
                  type: PacketType.ResponseError,
                  id: packet.id,
                  error: error instanceof Error ? error.message : `${error}`
                })
              }
            } finally {
              incomingRequests.delete(packet.id)
            }
          })()

          break
        }

        case PacketType.RequestCancel: {
          const cancel = incomingRequests.get(packet.id)
          if (cancel == null) {
            break
          }

          cancel()
          break
        }

        case PacketType.Response: {
          const response = incomingResponses.get(packet.id)
          if (response == null) {
            break
          }

          const { resolve } = response

          resolve(packet.data)
          break
        }

        case PacketType.ResponseCancel: {
          const response = incomingResponses.get(packet.id)
          if (response == null) {
            break
          }

          const { reject } = response

          reject(new ClientResponseCancelledError())
          break
        }

        case PacketType.ResponseError: {
          const response = incomingResponses.get(packet.id)
          if (response == null) {
            break
          }

          const { reject } = response

          reject(new ClientRequestError(packet.error))
          break
        }

        case PacketType.ResponseInternalError: {
          const response = incomingResponses.get(packet.id)
          if (response == null) {
            break
          }

          const { reject } = response

          reject(new ClientInternalResponseError(packet.reason))
          break
        }
      }
    })

    socket.io.on('open', () => {
      state.set([ClientState.Connected])
    })

    function onClose(error: Error = new Error('Failed to establish a connection to the server.')) {
      if (get(state)[0] === ClientState.Failed) {
        return
      }

      state.set([ClientState.Failed, error, () => connect()])
    }

    socket.io.on('close', (reason) => {
      onClose(new Error(reason))
    })

    socket.io.on('error', (error: Error) => {
      onClose(error)
    })

    socket.connect()
  }

  const preRequest: {
    run: () => Promise<void>
    reject: (reason: Error) => void
  }[] = []

  const storedAuthenticationToken: Writable<AuthenticationToken | null> = persisted(
    'stored-token',
    null
  )

  const notificationContext: Writable<NotificationContext | null> = writable(null)

  const context = setContext(clientContextName, {
    clientState: derived(proxiedSate, (value) => value),

    setHandler: async (
      code: ServerSideRequestCode,
      handler: (data: any) => Promise<Payload<ResponseCode>>
    ) => {
      if (requestHandlers.has(code)) {
        throw new Error('Already registered')
      }

      requestHandlers.set(code, handler)
      return () => requestHandlers.delete(code)
    },

    [requestSymbol]: async (
      state: Readable<ClientStateWritable>,
      code: ServerSideRequestCode,
      data: any
    ): Promise<any> => {
      const request = async (): Promise<any> => {
        const id = nextRequestId++

        try {
          return await new Promise<any>((resolve, reject) => {
            incomingResponses.set(id, {
              resolve: (value: Payload<ResponseCode>) => {
                if (value.Code === ResponseCode.OK) {
                  resolve(value.Data)
                } else {
                  reject(new ClientResponseError(code, value.Code))
                }
              },
              reject
            })

            send({
              type: PacketType.Request,
              id,
              data: {
                Code: code,
                Data: data
              }
            })
          })
        } catch (error: any) {
          throw new Error(error.message, { cause: error })
        }
      }

      if (get(state)[0] === ClientState.Connected) {
        return await request()
      } else if (get(state)[0] === ClientState.Failed) {
        throw get(state)[1]
      } else if (get(state)[0] === ClientState.Connecting) {
        return await new Promise((resolve, reject) => {
          preRequest.push({
            run: () => request().then(resolve, reject),
            reject
          })
        })
      } else {
        throw new Error('Unknown state')
      }
    },

    get [requestFunctionsSymbol]() {
      return getServerFunctions(context, storedAuthenticationToken)
    },

    authentication: derived(storedAuthenticationToken, (value) => value),

    setNotificationContext: (context: NotificationContext) => {
      notificationContext.update((value) => {
        if (value != null) {
          throw new Error('Notification context is already set')
        }

        return context
      })

      return () =>
        notificationContext.update((value) => {
          if (value != context) {
            throw new Error('Notification context mismatch')
          }

          return null
        })
    }
  })

  proxiedSate.subscribe((state) => {
    if (state[0] === ClientState.Connected) {
      for (let index = 0; index < preRequest.length; index++) {
        const { run } = preRequest[index]
        preRequest.splice(index--, 1)
        void run()
      }
    } else if (state[0] === ClientState.Failed) {
      const error = new Error('Failed to connect')

      for (let index = 0; index < preRequest.length; index++) {
        const { reject } = preRequest[index]
        preRequest.splice(index--, 1)
        reject(error)
      }

      for (const [, value] of incomingResponses) {
        const { reject } = value
        reject(error)
      }
    } else if (state[0] === ClientState.Connecting) {
      //
    }
  })

  state.subscribe(async (currentState) => {
    if (currentState[0] === ClientState.Connected) {
      const { authenticateToken } = getServerFunctions(context, storedAuthenticationToken, state)
      const token = get(storedAuthenticationToken)

      if (!(token == null || token.userId == null || token.token == null)) {
        try {
          await authenticateToken(token.userId, token.token)
        } catch (error: any) {
          if (error.message.includes('Invalid user id.')) {
            storedAuthenticationToken.set(null)
          }
        }
      } else {
        storedAuthenticationToken.set(null)
      }
    }

    proxiedSate.set(currentState)
  })

  connect()

  setClientRequestHandlers(requestHandlers, notificationContext)

  return {
    context
  }
}

export function useClientContext() {
  return getContext<ClientContext>(clientContextName)
}

export function useServerContext(): ServerSideContext {
  return useClientContext()[requestFunctionsSymbol]
}

export class ClientError extends Error {
  public constructor(message?: string, options?: ErrorOptions) {
    super(message, options)
  }
}

export class ClientRequestError extends ClientError {
  public constructor(errorData: string = 'Remote sent an error', options?: ErrorOptions) {
    super(errorData, options)

    this.#errorData = errorData
  }

  readonly #errorData: string

  get errorData() {
    return this.#errorData
  }
}

export class ClientResponseError extends ClientError {
  public constructor(requestCode: ServerSideRequestCode, responseCode: ResponseCode) {
    super(`Server responded with code: ${ResponseCode[responseCode]}`)

    this.#requestCode = requestCode
    this.#responseCode = responseCode
  }

  readonly #requestCode: ServerSideRequestCode
  readonly #responseCode: ResponseCode

  get requestCode() {
    return this.#requestCode
  }

  get responseCode() {
    return this.#responseCode
  }
}

export class ClientResponseCancelledError extends ClientError {
  public constructor(options?: ErrorOptions) {
    super(`Remote cancelled the request`, options)
  }
}

export class ClientInternalResponseError extends Error {
  public constructor(reason: number, options?: ErrorOptions) {
    super(`Remote returned an internal error`, options)

    this.#reason = reason
  }

  readonly #reason: number

  get reason() {
    return this.#reason
  }
}

export interface AuthenticationToken {
  userId: string
  token: string
}

export type ServerSideContext = ReturnType<typeof getServerFunctions>

function setClientRequestHandlers(
  map: Map<number, (data: any, isCancelled: () => boolean) => Promise<Payload<ResponseCode>>>,
  notification: Readable<NotificationContext | null>
) {
  const setHandler = <C extends ClientSideRequestCode, T extends object, R extends object>(
    code: C,
    handler: (data: T) => Promise<R>
  ) => {
    map.set(code, async (data) => {
      try {
        return {
          Code: ResponseCode.OK,
          Data: await handler(data)
        }
      } catch (error: any) {
        if (error instanceof ClientResponseError) {
          return {
            Code: error.responseCode,
            Data: {}
          }
        }

        return {
          Code: ResponseCode.InternalError,
          Data: {}
        }
      }
    })
  }

  setHandler(ClientSideRequestCode.Ping, async (data) => {
    return {}
  })

  setHandler(ClientSideRequestCode.Notify, async (data) => {
    const context = get(notification)

    if (context != null) {
      context.reload()
    }

    return {}
  })
}

function getServerFunctions(
  context: ClientContext,
  storedAuthenticationToken: Writable<AuthenticationToken | null>,
  state?: Writable<ClientStateWritable>
) {
  const { [requestSymbol]: requestInternal } = context

  const request = requestInternal.bind(undefined, state ?? context.clientState)

  const functions = {
    getSetupRequirements: (): Promise<{
      adminSetupRequired: boolean
    }> => request(ServerSideRequestCode.SetupRequirements, {}),

    createAdmin: (
      username: string,
      password: string,
      confirmPassword: string,
      firstName: string,
      middleName: string | null,
      lastName: string,
      displayName: string | null
    ): Promise<object> =>
      request(ServerSideRequestCode.CreateAdmin, {
        username,
        password,
        confirmPassword,
        lastName,
        firstName,
        middleName,
        displayName
      }),

    resolveUsername: async (username: string): Promise<string | null> => {
      const result = await request(ServerSideRequestCode.ResolveUsername, {
        username
      })
      return result.userId
    },

    authenticatePassword: async (
      userId: string,
      password: string
    ): Promise<{
      token: string
      userId: string
    }> => {
      const { token } = await request(ServerSideRequestCode.AuthenticatePassword, {
        userId,
        password
      })

      storedAuthenticationToken.set({
        userId,
        token
      })

      return {
        token,
        userId
      }
    },

    authenticateGoogle: async (googleToken: string) => {
      const { token, userId } = await request(ServerSideRequestCode.AuthenticateGoogle, {
        googleToken
      })

      storedAuthenticationToken.set({
        userId,
        token
      })

      return {
        token,
        userId
      }
    },

    authenticateToken: async (userId: string, token: string): Promise<void> => {
      const { renewedToken } = await request(ServerSideRequestCode.AuthenticateToken, {
        userId,
        token
      })

      if (renewedToken != null) {
        storedAuthenticationToken.set({
          userId,
          token: renewedToken
        })
      }
    },

    deauthenticate: async () => {
      const result = await request(ServerSideRequestCode.Deauthenticate, {})

      storedAuthenticationToken.set(null)

      return result
    },

    me: async (): Promise<UserResource> => {
      const { user } = await request(ServerSideRequestCode.Me, {})

      return JSON.parse(user) as UserResource
    },

    amILoggedIn: async () => {
      const { isLoggedIn } = await request(ServerSideRequestCode.AmILoggedIn, {})

      return isLoggedIn as boolean
    },

    getUser: async (userId: string): Promise<UserResource | null> => {
      const result = await request(ServerSideRequestCode.GetUser, {
        userId
      })

      if (result.user != null) {
        return JSON.parse(result.user)
      }

      return null
    },

    getUsers: async ({
      searchString,
      includeRole,
      excludeRole,
      username,
      id,
      offset,
      count,
      excludeSelf
    }: {
      searchString?: string
      includeRole?: UserRole[]
      excludeRole?: UserRole[]
      username?: string
      id?: string
      offset?: number
      count?: number
      excludeSelf?: boolean
    }) => {
      const { users } = await request(ServerSideRequestCode.GetUsers, {
        searchString,
        includeRole,
        excludeRole,
        username,
        id,
        pagination: {
          offset,
          count
        },
        excludeSelf
      })

      return users.map((user: any) => JSON.parse(user)) as UserResource[]
    },

    getFile: async (fileId?: string) => {
      const { file } = await request(ServerSideRequestCode.GetFile, {
        fileId
      })

      return JSON.parse(file) as FileResource
    },

    getFiles: async (
      parentFolderId?: string,
      fileType?: FileType,
      name?: string,
      ownerUserId?: string,
      trashOptions?: TrashOptions,
      offset?: number,
      count?: number
    ) => {
      const { files } = await request(ServerSideRequestCode.GetFiles, {
        parentFolderId,
        fileType,
        name,
        ownerUserId,
        trashOptions,
        pagination: {
          offset,
          count
        }
      })

      return files.map((file: any) => JSON.parse(file)) as FileResource[]
    },

    getFileAccesses: async ({
      targetFileId,
      targetUserId,
      authorUserId,
      level,
      id,
      offset,
      count,
      includePublic
    }: {
      targetUserId?: string
      targetFileId?: string
      authorUserId?: string
      level?: FileAccessLevel
      id?: string
      offset?: number
      count?: number
      includePublic?: boolean
    }) => {
      const { fileAccesses } = await request(ServerSideRequestCode.GetFileAccesses, {
        targetUserId,
        targetFileId,
        authorUserId,
        level,
        id,
        pagination: {
          offset,
          count
        },
        includePublic
      })

      return fileAccesses.map((fileAccess: any) => JSON.parse(fileAccess)) as FileAccessResource[]
    },

    getFileStars: async (fileId?: string, userId?: string, offset?: number, count?: number) => {
      const { fileStars } = await request(ServerSideRequestCode.GetFileStars, {
        fileId,
        userId,
        pagination: {
          offset,
          count
        }
      })

      return fileStars.map((fileStar: any) => JSON.parse(fileStar)) as FileStarResource[]
    },

    getFilePath: async (fileId?: string, offset?: number, count?: string) => {
      const { path } = await request(ServerSideRequestCode.GetFilePath, {
        fileId,
        pagination: {
          offset,
          count
        }
      })

      return path.map((entry: any) => JSON.parse(entry)) as FileResource[]
    },

    createFolder: async (fileId: string, name: string) => {
      const { file } = await request(ServerSideRequestCode.CreateFolder, {
        fileId,
        name
      })

      return JSON.parse(file) as FileResource
    },

    getFileMime: async (fileId: string, fileContentId?: string, fileSnapshotId?: string) => {
      const { fileMimeType } = await request(ServerSideRequestCode.GetFileMime, {
        fileId,
        fileContentId,
        fileSnapshotId
      })

      return fileMimeType as string
    },

    getFileContents: async (
      fileId?: string,
      fileContentId?: string,
      offset?: number,
      count?: number
    ) => {
      const { fileContents } = await request(ServerSideRequestCode.GetFileContents, {
        fileId,
        fileContentId,
        pagination: {
          offset,
          count
        }
      })

      return fileContents.map((fileContent: any) =>
        JSON.parse(fileContent)
      ) as FileContentResource[]
    },

    getMainFileContent: async (fileId: string) => {
      const { fileContent } = await request(ServerSideRequestCode.GetMainFileContent, {
        fileId
      })

      return JSON.parse(fileContent) as FileContentResource
    },

    getFileSnapshots: async (
      fileId: string,
      fileContentId?: string,
      fileSnapshotId?: string,
      offset?: number,
      count?: number
    ) => {
      const { fileSnapshots } = await request(ServerSideRequestCode.GetFileSnapshots, {
        fileId,
        fileContentId,
        fileSnapshotId,
        pagination: {
          offset,
          count
        }
      })

      return fileSnapshots.map((fileSnapshot: string) =>
        JSON.parse(fileSnapshot)
      ) as FileSnapshotResource[]
    },

    amIAdmin: async () => {
      const { isAdmin } = await request(ServerSideRequestCode.AmIAdmin, {})

      return isAdmin as number
    },

    getFileLogs: async ({
      fileId,
      fileContentId,
      fileSnapshotId,
      userId,
      offset,
      count
    }: {
      fileId?: string
      fileContentId?: string
      fileSnapshotId?: string
      userId?: string
      offset?: number
      count?: number
    }) => {
      const { fileLogs } = await request(ServerSideRequestCode.GetFileLogs, {
        fileId,
        fileContentId,
        fileSnapshotId,
        userId,
        pagination: {
          offset,
          count
        }
      })

      return fileLogs.map((fileLog: any) => JSON.parse(fileLog)) as FileLogResource[]
    },

    getFileSize: async (fileId: string, fileContentId?: string, fileSnapshotId?: string) => {
      const { size } = await request(ServerSideRequestCode.GetFileSize, {
        fileId,
        fileContentId,
        fileSnapshotId
      })

      return size as number
    },

    scanFile: async (fileId: string, fileContentId: string, fileSnapshotId: string) => {
      const { result } = await request(ServerSideRequestCode.ScanFile, {
        fileId,
        fileContentId,
        fileSnapshotId
      })

      return JSON.parse(result) as VirusReportResource
    },

    createFile: async (fileId: string, name: string) => {
      const { streamId } = await request(ServerSideRequestCode.CreateFile, {
        fileId,
        name
      })

      return streamId as string
    },

    openStream: async (fileId: string, fileContentId: string, fileSnapshotId: string) => {
      const { streamId } = await request(ServerSideRequestCode.OpenStream, {
        fileId,
        fileContentId,
        fileSnapshotId
      })

      return streamId as string
    },

    closeStream: async (streamId: string) => {
      await request(ServerSideRequestCode.CloseStream, {
        streamId
      })
    },

    readStream: async (streamId: string, length: number) => {
      const { data } = await request(ServerSideRequestCode.ReadStream, {
        streamId,
        length
      })

      return Buffer.from(data, 'base64')
    },

    writeStream: async (streamId: string, data: Buffer | Blob) => {
      await request(ServerSideRequestCode.WriteStream, {
        streamId,
        data
      })
    },

    setPosition: async (streamId: string, newPosition: number) => {
      await request(ServerSideRequestCode.SetPosition, {
        streamId,
        newPosition
      })
    },

    getStreamSize: async (streamId: string) => {
      const { size } = await request(ServerSideRequestCode.GetStreamSize, {
        streamId
      })

      return size as number
    },

    getStreamPosition: async (streamId: string) => {
      const { position } = await request(ServerSideRequestCode.GetStreamPosition, {
        streamId
      })

      return position as number
    },

    createNews: async (title: string, imageFileIds: string[], publishTime?: Date) => {
      const { news } = await request(ServerSideRequestCode.CreateNews, {
        title,
        imageFileIds,
        publishTime
      })

      return JSON.parse(news) as NewsResource
    },

    deleteNews: async (newsId: string) => {
      await request(ServerSideRequestCode.DeleteNews, {
        newsId
      })
    },

    getNews: async (afterId?: string, published?: boolean, offset?: number, count?: number) => {
      const { newsEntries } = await request(ServerSideRequestCode.GetNews, {
        afterId,
        published,
        pagination: {
          offset,
          count
        }
      })

      return newsEntries.map((newsEntry: any) => JSON.parse(newsEntry)) as NewsResource[]
    },

    getLatestFileSnapshot: async (fileId: string, fileContentId?: string) => {
      const { fileSnapshot } = await request(ServerSideRequestCode.GetLatestFileSnapshot, {
        fileId,
        fileContentId
      })

      return fileSnapshot != null ? (JSON.parse(fileSnapshot) as FileSnapshotResource) : null
    },

    getOldestFileSnapshot: async (fileId: string, fileContentId?: string) => {
      const { fileSnapshot } = await request(ServerSideRequestCode.GetOldestFileSnapshot, {
        fileId,
        fileContentId
      })

      return fileSnapshot != null ? (JSON.parse(fileSnapshot) as FileSnapshotResource) : null
    },

    setFileStar: async (fileId: string, starred: boolean) => {
      await request(ServerSideRequestCode.SetFileStar, {
        fileId,
        starred
      })
    },

    getFileStar: async (fileId: string) => {
      const { starred } = await request(ServerSideRequestCode.GetFileStar, {
        fileId
      })

      return starred as boolean
    },

    didIAgree: async () => {
      const { agreed } = await request(ServerSideRequestCode.DidIAgree, {})

      return agreed as boolean
    },

    agree: async () => {
      await request(ServerSideRequestCode.Agree, {})
    },

    createUser: async ({
      username,
      firstName,
      middleName,
      lastName,
      displayName,
      password
    }: {
      username: string
      password?: string
      firstName: string
      middleName?: string
      lastName: string
      displayName?: string
    }): Promise<{ password: string; userId: string }> => {
      const { password: passwordResult, userId } = await request(ServerSideRequestCode.CreateUser, {
        username,
        firstName,
        middleName,
        lastName,
        displayName,
        password
      })

      return { password: passwordResult, userId }
    },

    getUsernameValidationFlags: async (username: string) => {
      const { flags } = await request(ServerSideRequestCode.GetUsernameValidationFlags, {
        username
      })

      return flags as UsernameValidationFlags
    },

    getPasswordValidationFlags: async (password: string) => {
      const { flags } = await request(ServerSideRequestCode.GetPasswordValidationFlags, {
        password
      })

      return flags as PasswordValidationFlags
    },

    setFileAccess: async (fileId: string, level: FileAccessLevel, targetUserId?: string) => {
      await request(ServerSideRequestCode.SetFileAccess, {
        fileId,
        targetUserId,
        level
      })
    },

    getFileAccessLevel: async (fileId: string) => {
      const { fileAccessLevel } = await request(ServerSideRequestCode.GetFileAccessLevel, {
        fileId
      })

      return fileAccessLevel as FileAccessLevel
    },

    requestPasswordReset: async (username: string) => {
      await request(ServerSideRequestCode.RequestPasswordReset, { username })
    },

    getPasswordResetRequests: async ({
      status,
      offset,
      count
    }: {
      status: PasswordResetRequestStatus
      offset?: number
      count?: string
    }) => {
      const { requests } = await request(ServerSideRequestCode.GetPasswordResetRequests, {
        status,
        pagination: { offset, count }
      })

      return requests.map((data: any) => JSON.parse(data)) as PasswordResetRequestResource[]
    },

    declinePasswordResetRequest: async (passwordResetRequestId: string) => {
      await request(ServerSideRequestCode.DeclinePasswordResetRequest, { passwordResetRequestId })
    },

    acceptPasswordResetRequest: async (passwordResetRequestId: string, newPassword: string) => {
      await request(ServerSideRequestCode.AcceptPasswordResetRequest, {
        passwordResetRequestId,
        password: newPassword
      })
    },

    isUserAdmin: async (userId: string) => {
      const { isAdmin } = await request(ServerSideRequestCode.IsUserAdmin, { userId })

      return isAdmin as boolean
    },

    setUserRoles: async (userId: string, roles: UserRole[]) => {
      await request(ServerSideRequestCode.SetUserRoles, { userId, roles })
    },

    transcribeAudio: async (fileId: string, fileSnapshotId?: string) => {
      const { text, status } = await request(ServerSideRequestCode.TranscribeAudio, {
        fileId,
        fileSnapshotId
      })

      return {
        text: text as string[],
        status: status as AudioTranscriptionStatus
      }
    },

    trashFile: async (fileId: string) => {
      await request(ServerSideRequestCode.TrashFile, { fileId })
    },

    untrashFile: async (fileId: string) => {
      await request(ServerSideRequestCode.UntrashFile, { fileId })
    },

    updateUsername: async (username: string) => {
      await request(ServerSideRequestCode.UpdateUsername, { newUsername: username })
    }
  }

  return functions
}

export interface PasswordResetRequestResource extends ResourceData {
  userId: string
  status: PasswordResetRequestStatus
}

export interface VirusReportResource extends ResourceData {
  fileId: string
  fileContentId: string
  fileSnapshotId: string
  status: VirusReportStatus
  viruses: string[]
}

export enum ClientSideRequestCode {
  Ping,
  Notify
}
export enum AudioTranscriptionStatus {
  NotRunning,
  Pending,
  Running,
  Done,
  Error
}

export enum ResponseCode {
  OK,
  Cancelled,
  InvalidParameters,
  NoHandlerFound,
  InvalidRequestCode,
  InternalError,
  AuthenticationRequired,
  AgreementRequired,
  InsufficientRole,
  ResourceNotFound,
  Forbidden,
  FileNameConflict
}

export enum ServerSideRequestCode {
  Echo,

  Me,
  AmILoggedIn,
  AmIAdmin,

  AuthenticatePassword,
  AuthenticateGoogle,
  AuthenticateToken,
  Deauthenticate,
  GetAgreement,

  CreateAdmin,
  SetupRequirements,

  ResolveUsername,

  GetUser,
  GetUsers,

  GetFile,
  GetFiles,
  GetFileAccesses,
  GetFileStars,
  GetFilePath,
  GetFileMime,
  GetFileSize,
  GetFileContents,
  GetMainFileContent,
  GetFileSnapshots,
  GetLatestFileSnapshot,
  GetOldestFileSnapshot,
  GetFileLogs,
  ScanFile,
  CreateFolder,
  CreateFile,
  SetFileStar,
  GetFileStar,

  OpenStream,
  CloseStream,
  ReadStream,
  WriteStream,
  SetPosition,
  GetStreamSize,
  GetStreamPosition,

  CreateNews,
  DeleteNews,
  GetNews,

  DidIAgree,
  Agree,

  TrashFile,
  UntrashFile,
  MoveFile,
  CreateUser,

  GetUsernameValidationFlags,
  GetPasswordValidationFlags,

  SetFileAccess,
  GetFileAccessLevel,

  RequestPasswordReset,
  GetPasswordResetRequests,
  DeclinePasswordResetRequest,
  AcceptPasswordResetRequest,

  IsUserAdmin,
  SetUserRoles,

  TranscribeAudio,

  UpdateUsername
}

export enum PasswordResetRequestStatus {
  Pending,
  Accepted,
  Declined
}

export enum UsernameValidationFlags {
  OK = 0,
  TooShort = 1 << 0,
  TooLong = 1 << 1,
  InvalidChars = 1 << 2
}

export enum PasswordValidationFlags {
  OK = 0,
  TooShort = 1 << 0,
  TooLong = 1 << 1,
  NoRequiredChars = 1 << 2,
  PasswordMismatch = 1 << 3
}

export interface SearchParams {
  count?: number
  offset?: number
}

export enum UserRole {
  None = 0,
  Admin = 1 << 0,
  NewsEditor = 1 << 1
}

export interface ResourceData {
  id: string
}

export interface UserResource extends ResourceData {
  username: string
  firstName: string
  middleName?: string
  lastName: string
  displayName?: string
  roles: UserRole[]
}

export interface FileResource extends ResourceData {
  parentId?: string
  ownerUserId: string
  name: string
  type: FileType
  trashTime?: Date
}

export interface FileAccessResource extends ResourceData {
  fileId: string
  authorUserId: string
  targetUserId?: string
  level: FileAccessLevel
}

export interface FileSnapshotResource extends ResourceData {
  createTime: Date
  fileId: string
  fileContentId: string
  authorUserId: string
  baseFileSnapshotId?: string
  size: number
}

export interface FileLogResource extends ResourceData {
  type: FileType
  fileId: string
  actorUserId?: string
  fileContentId?: string
  fileSnapshotId?: string
}

export interface FileVirusReportResource extends ResourceData {
  fileId: string
  fileContentId: string
  fileSnapshotId: string
  viruses: string[]
}

export interface FileStarResource extends ResourceData {
  fileId: string
  userId: string

  creatTime: Date
}

export interface FileContentResource extends ResourceData {
  fileId: string
  main: boolean
  name: string
}

export interface NewsResource extends ResourceData {
  title: string
  imageFileIds: string[]
  authorUserId: string
}

export enum FileAccessLevel {
  None,
  Read,
  ReadWrite,
  Manage,
  Full
}

export enum FileType {
  File,
  Folder
}

export enum TrashOptions {
  NotIncluded,
  Included,
  Exclusive
}

export enum FileLogType {
  CreateFile,
  TrashFile,
  ModifyFile
}

export enum VirusReportStatus {
  Pending,
  Failed,
  Completed
}
