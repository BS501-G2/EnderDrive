/* eslint-disable @typescript-eslint/no-empty-object-type */
/* eslint-disable @typescript-eslint/no-explicit-any */
import { getContext, setContext } from 'svelte'
import { derived, get, writable, type Writable } from 'svelte/store'

import { persisted } from 'svelte-persisted-store'
import { type ConnectionState, createConnection } from './connection'
import type {
  AudioTranscriptionStatus,
  FileAccessLevel,
  FileAccessResource,
  FileDataResource,
  FileLogResource,
  FileNameValidationFlags,
  FileResource,
  FileStarResource,
  FileType,
  NewsResource,
  PasswordResetRequestResource,
  PasswordResetRequestStatus,
  PasswordValidationFlags,
  TrashOptions,
  UsernameValidationFlags,
  UserResource,
  UserRole,
  VirusReportResource
} from './resource'

const name = Symbol('Client Context')

export type ClientContext = ReturnType<typeof createClientContext>['context']

export type RemoteFunction<S, R, T = undefined> = [request: S, response: R, translator?: T]

export interface Authentication {
  UserId: string
  Token: string
}

export type FunctionMap<T extends ServerSideFunctions | ClientSideFunctions> = {
  [key in keyof T]: (request: T[key][0]) => Promise<T[key][1]>
}

export type TranslatorMap<T extends ServerSideFunctions | ClientSideFunctions> = {
  [key in keyof T]: T[key][2] extends undefined
    ? (data: T[key][1]) => T[key][1]
    : (data: T[key][2]) => T[key][1]
}

export type CustomFunctionMap<T extends ServerSideFunctions | ClientSideFunctions> = {
  [key in keyof FunctionMap<T>]: (
    data: T[key][0],
    request: FunctionMap<T>[key]
  ) => ReturnType<FunctionMap<T>[key]>
}

export interface ServerSideFunctions extends Record<string, RemoteFunction<any, any, any>> {
  AcceptPasswordResetRequest: [
    {
      PasswordResetRequestId: string
      Password: string
    },
    object
  ]
  Agree: [object, object]
  AmIAdmin: [object, boolean, { IsAdmin: boolean }]
  AmILoggedIn: [object, boolean, { IsLoggedIn: boolean }]
  AuthenticateGoogle: [{ Token: string }, { UserId: string; Token: string }]
  AuthenticatePassword: [{ UserId: string; Password: string }, { UserId: string; Token: string }]
  AuthenticateToken: [
    { UserId: string; Token: string },
    string | undefined,
    { RenewedToken?: string }
  ]
  CreateAdmin: [
    {
      Username: string
      Password: string
      ConfirmPassword: string
      LastName: string
      FirstName: string
      MiddleName?: string
      DisplayName?: string
    },
    object
  ]
  FolderCreate: [{ FileId: string; Name: string }, FileResource, { File: string }]
  CreateNews: [
    { Title: string; ImageFileId: string; PublishTime?: number },
    NewsResource,
    {
      News: string
    }
  ]
  CreateUser: [
    {
      Username: string
      FirstName: string
      MiddleName?: string
      LastName: string
      DisplayName?: string
      Password?: string
    },
    {
      UserId: string
      Password: string
    }
  ]
  Deauthenticate: [{}, {}]
  DeclinePasswordResetRequest: [{ PasswordResetRequestId: string }, {}]
  DeleteNews: [
    {
      FileId: string
    },
    {}
  ]
  DidIAgree: [{}, boolean, { Agreed: boolean }]
  FileCreate: [
    { FileId: string; Name: string },
    string,
    {
      StreamId: string
    }
  ]
  FileDelete: [
    {
      FileId: string
    },
    {}
  ]
  FileScan: [{ FileId: string; FileDataId: string }, VirusReportResource, { Result: string }]
  FileGetMime: [
    { FileId: string; FileDataId?: string },
    string,
    {
      MimeType: string
    }
  ]
  FileGetDataEntries: [
    { FileId: string; FileDataId?: string; Pagination?: PaginationOptions },
    FileDataResource[],
    {
      FileDataEntries: string[]
    }
  ]
  FileGetSize: [{ FileId: string; FileDataId?: string }, number, { Size: number }]
  GetFile: [
    {
      FileId: string
    },
    FileResource,
    {
      File: string
    }
  ]
  GetFileAccesses: [
    {
      TargetUserId?: string
      TargetFileId?: string
      AuthorUserId?: string
      Level?: FileAccessLevel
      Pagination?: PaginationOptions
      IncludePublic: boolean
    },
    FileAccessResource[],
    {
      FileAccesses: string[]
    }
  ]
  GetFileAccessLevel: [
    {
      FileId: string
    },
    FileAccessLevel,
    {
      Level: FileAccessLevel
    }
  ]
  GetFileLogs: [
    {
      FileId?: string
      FileDataId?: string
      UserId?: string
      Pagination?: PaginationOptions
      UniqueFileId: boolean
    },
    FileLogResource[],
    {
      FileLogs: string[]
    }
  ]
  GetFilePath: [
    { FileId: string; Pagination?: PaginationOptions },
    FileResource[],
    { Path: string[] }
  ]
  GetFiles: [
    {
      SearchString?: string
      ParentFolderId?: string
      FileType?: FileType
      OwnerUserId?: string
      Name?: string
      Id?: string
      TrashOptions?: TrashOptions
      Pagination?: PaginationOptions
    },
    FileResource[],
    {
      Files: string[]
    }
  ]
  GetFileStar: [{ FileId: string }, boolean, { Starred: boolean }]
  GetFileStars: [
    { FileId?: string; UserId?: string; Pagination?: PaginationOptions },
    FileStarResource[],
    { FileStars: string[] }
  ]
  GetFileNameValidationFlags: [
    { Name: string; ParentId: string },
    FileNameValidationFlags,
    {
      Flags: FileNameValidationFlags
    }
  ]
  GetNews: [
    { Pagination?: PaginationOptions; AfterId?: string; Published?: boolean },
    string[],
    {
      NewsIds: string[]
    }
  ]
  GetPasswordResetRequests: [
    {
      Status?: PasswordResetRequestStatus
      Pagination?: PaginationOptions
    },
    PasswordResetRequestResource[],
    {
      Requests: string[]
    }
  ]
  GetPasswordValidationFlags: [
    {
      Password: string
      ConfirmPassword?: string
    },
    PasswordValidationFlags,
    {
      Flags: PasswordValidationFlags
    }
  ]
  GetRootId: [
    {
      UserId: string
    },
    string | undefined,
    {
      FileId?: string
    }
  ]
  GetUser: [
    {
      UserId: string
    },
    UserResource | undefined,
    {
      User?: string
    }
  ]
  GetUsernameValidationFlags: [
    { Username: string },
    UsernameValidationFlags,
    { Flags: UsernameValidationFlags }
  ]
  GetUsers: [
    {
      SearchString?: string
      IncludeRole?: UserRole[]
      ExcludeRole?: UserRole[]
      Username?: string
      Id?: string
      Pagination?: PaginationOptions
      ExcludeSelf?: boolean
    },
    UserResource[],
    { Users: string[] }
  ]
  IsUserAdmin: [{ UserId: string }, boolean, { IsAdmin: boolean }]
  MoveFile: [{ FileId: string; NewParentId?: string; NewName?: string }, {}]
  RequestPasswordReset: [{ UserId?: string; Username?: string }, {}]
  ResolveUsername: [{ Username: string }, string | undefined, { UserId?: string }]
  SetFileAccess: [{ FileId: string; TargetUserId?: string; Level: FileAccessLevel }, {}]
  SetFileStar: [{ FileId: string; Starred: boolean }, {}]
  SetupRequirements: [{}, { AdminSetupRequired: boolean }]
  SetUserRoles: [{ UserId: string; Roles: UserRole[] }, {}]

  StreamClose: [{ StreamId: string }, {}]
  StreamGetLength: [{ StreamId: string }, number, { Length: number }]
  StreamGetPosition: [{ StreamId: string }, number, { Position: number }]
  StreamOpen: [
    { FileId: string; FileDataId: string; ForWriting: boolean },
    string,
    {
      StreamId: string
    }
  ]
  StreamRead: [{ StreamId: string; Length: number }, Uint8Array, { Data: Uint8Array }]
  StreamSetLength: [{ StreamId: string; Length: number }, {}]
  StreamSetPosition: [{ StreamId: string; Position: number }, {}]
  StreamWrite: [{ StreamId: string; Data: Uint8Array | Blob }, {}]

  TranscribeAudio: [
    { FileId: string; FileDataId?: string },
    {
      Text: string[]
      Status: AudioTranscriptionStatus
    }
  ]

  TrashFile: [{ FileId: string }, {}]
  UntrashFile: [{ FileId: string }, {}]
  UpdateName: [
    { FirstName: string; MiddleName?: string; LastName: string; DisplayName?: string },
    {}
  ]
  UpdatePassword: [{ CurrentPassword: string; NewPassword: string; ConfirmPassword: string }, {}]
  UpdateUsername: [{ NewUsername: string }, {}]
  Me: [{}, UserResource, { User: string }]
}

export interface PaginationOptions {
  Count?: number
  Offset?: number
}

export interface ClientSideFunctions extends Record<string, RemoteFunction<any, any, any>> {
  Notify: [{}, object]
}

export function createClientContext() {
  const serverSideFunctionTranslators: TranslatorMap<ServerSideFunctions> = {
    AcceptPasswordResetRequest: (data) => data,
    Agree: (data) => data,
    AmIAdmin: (data) => data.IsAdmin,
    AmILoggedIn: (data) => data.IsLoggedIn,
    AuthenticateGoogle: (data) => data,
    AuthenticatePassword: (data) => data,
    AuthenticateToken: (data) => data.RenewedToken,
    CreateAdmin: (data) => data,
    FolderCreate: (data) => JSON.parse(data.File) as FileResource,
    CreateNews: (data) => JSON.parse(data.News) as NewsResource,
    CreateUser: (data) => data,
    Deauthenticate: (data) => data,
    DeclinePasswordResetRequest: (data) => data,
    DeleteNews: (data) => data,
    DidIAgree: (data) => data.Agreed,
    FileCreate: (data) => data.StreamId,
    FileDelete: (data) => data,
    FileScan: (data) => JSON.parse(data.Result) as VirusReportResource,
    FileGetMime: (data) => data.MimeType,
    FileGetSize: (data) => data.Size,
    GetFile: ({ File }) => JSON.parse(File) as FileResource,
    GetFileAccesses: (data) =>
      data.FileAccesses.map((fileAccess) => JSON.parse(fileAccess) as FileAccessResource),
    GetFileAccessLevel: (data) => data.Level,
    GetFileLogs: (data) => data.FileLogs.map((fileLog) => JSON.parse(fileLog) as FileLogResource),
    GetFilePath: (data) => data.Path.map((pathEntry) => JSON.parse(pathEntry) as FileResource),
    GetFiles: ({ Files }) => Files.map((file) => JSON.parse(file) as FileResource),
    GetFileStar: (data) => data.Starred,
    GetFileStars: ({ FileStars }) =>
      FileStars.map((fileStar) => JSON.parse(fileStar) as FileStarResource),
    GetFileNameValidationFlags: (data) => data.Flags,
    GetNews: (data) => data.NewsIds,
    GetPasswordResetRequests: ({ Requests }) =>
      Requests.map((request) => JSON.parse(request) as PasswordResetRequestResource),
    GetPasswordValidationFlags: (data) => data.Flags,
    GetRootId: (data) => data.FileId,
    GetUser: ({ User }) => (User != null ? (JSON.parse(User) as UserResource) : undefined),
    GetUsernameValidationFlags: (data) => data.Flags,
    GetUsers: ({ Users }) => Users.map((user) => JSON.parse(user) as UserResource),
    IsUserAdmin: (data) => data.IsAdmin,
    MoveFile: (data) => data,
    RequestPasswordReset: (data) => data,
    ResolveUsername: (data) => data.UserId,
    SetFileAccess: (data) => data,
    SetFileStar: (data) => data,
    SetupRequirements: (data) => data,
    SetUserRoles: (data) => data,

    StreamClose: (data) => data,
    StreamGetLength: (data) => data.Length,
    StreamGetPosition: (data) => data.Position,
    StreamOpen: (data) => data.StreamId,
    StreamRead: (data) => data.Data,
    StreamSetLength: (data) => data,
    StreamSetPosition: (data) => data,
    StreamWrite: (data) => data,

    TranscribeAudio: (data) => data,

    TrashFile: (data) => data,
    UntrashFile: (data) => data,
    UpdateName: (data) => data,
    UpdatePassword: (data) => data,
    UpdateUsername: (data) => data,
    Me: ({ User }) => JSON.parse(User) as UserResource,
    FileGetDataEntries: ({ FileDataEntries }) =>
      FileDataEntries.map((fileData) => JSON.parse(fileData) as FileDataResource)
  }

  const requestHandlers: Partial<FunctionMap<ClientSideFunctions>> = {}

  const connection = createConnection((name, data) => requestHandlers[name]!(data as never))

  const authentication: Writable<Authentication | null> = persisted('stored-token', null as never)

  const clientState = writable<ConnectionState>(['connecting'])

  connection.state.subscribe(async (state) => {
    console.log('asd')
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
      connection.sendRequest(name as string, data).then(serverSideFunctionTranslators[name])

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

  return { context, authentication }
}

export function useClientContext() {
  return getContext<ClientContext>(name)
}
