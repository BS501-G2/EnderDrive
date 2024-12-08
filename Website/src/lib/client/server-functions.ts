/* eslint-disable @typescript-eslint/no-empty-object-type */
/* eslint-disable @typescript-eslint/no-explicit-any */

import type { RemoteFunction, PaginationOptions, TranslatorMap } from './client'
import type {
  FileResource,
  NewsResource,
  VirusReportResource,
  FileDataResource,
  FileAccessLevel,
  FileAccessResource,
  FileLogResource,
  FileType,
  TrashOptions,
  FileStarResource,
  FileNameValidationFlags,
  PasswordResetRequestStatus,
  PasswordResetRequestResource,
  PasswordValidationFlags,
  UserResource,
  UsernameValidationFlags,
  UserRole,
  AudioTranscriptionStatus,
  NotificationResource
} from './resource'

export interface ServerSideFunctions extends Record<string, RemoteFunction<any, any, any, any>> {
  AcceptPasswordResetRequest: RemoteFunction<
    {
      PasswordResetRequestId: string
      Password: string
    },
    object
  >
  Agree: RemoteFunction<object, object>
  AmIAdmin: RemoteFunction<object, boolean, void, { IsAdmin: boolean }>
  AmILoggedIn: RemoteFunction<{}, boolean, void, { IsLoggedIn: boolean }>
  AuthenticateGoogle: RemoteFunction<{ Token: string }, { UserId: string; Token: string }>
  AuthenticatePassword: RemoteFunction<
    { UserId: string; Password: string },
    { UserId: string; Token: string }
  >
  AuthenticateToken: RemoteFunction<
    { UserId: string; Token: string },
    string | undefined,
    void,
    { RenewedToken?: string }
  >
  CreateAdmin: RemoteFunction<
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
  >
  FolderCreate: RemoteFunction<
    { FileId: string; Name: string },
    FileResource,
    void,
    { File: string }
  >
  CreateNews: RemoteFunction<
    { Title: string; ImageFileId: string; PublishTime?: number },
    NewsResource,
    void,
    {
      News: string
    }
  >
  CreateUser: RemoteFunction<
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
  >
  Deauthenticate: RemoteFunction<{}, {}>
  DeclinePasswordResetRequest: RemoteFunction<{ PasswordResetRequestId: string }, {}>
  DeleteNews: RemoteFunction<
    {
      NewsId: string
    },
    {}
  >
  DidIAgree: RemoteFunction<{}, boolean, void, { Agreed: boolean }>
  FileCreate: RemoteFunction<
    { FileId: string; Name: string },
    string,
    void,
    {
      StreamId: string
    }
  >
  FileDelete: RemoteFunction<
    {
      FileId: string
    },
    {}
  >
  FileScan: RemoteFunction<
    { FileId: string; FileDataId: string },
    VirusReportResource,
    void,
    { Result: string }
  >
  FileGetMime: RemoteFunction<
    { FileId: string; FileDataId?: string },
    string,
    void,
    {
      MimeType: string
    }
  >
  FileGetDataEntries: RemoteFunction<
    { FileId: string; FileDataId?: string; Pagination?: PaginationOptions },
    FileDataResource[],
    void,
    {
      FileDataEntries: string[]
    }
  >
  FileGetSize: RemoteFunction<
    { FileId: string; FileDataId?: string },
    number,
    void,
    { Size: number }
  >
  GetFile: RemoteFunction<
    {
      FileId: string
    },
    FileResource,
    void,
    {
      File: string
    }
  >
  GetFileAccesses: RemoteFunction<
    {
      TargetUserId?: string
      TargetFileId?: string
      AuthorUserId?: string
      Level?: FileAccessLevel
      Pagination?: PaginationOptions
      IncludePublic: boolean
    },
    FileAccessResource[],
    void,
    {
      FileAccesses: string[]
    }
  >
  GetFileAccessLevel: RemoteFunction<
    {
      FileId: string
    },
    FileAccessLevel,
    void,
    {
      Level: FileAccessLevel
    }
  >
  GetFileLogs: RemoteFunction<
    {
      FileId?: string
      FileDataId?: string
      UserId?: string
      Pagination?: PaginationOptions
      UniqueFileId: boolean
    },
    FileLogResource[],
    void,
    {
      FileLogs: string[]
    }
  >
  GetFilePath: RemoteFunction<
    { FileId: string; Pagination?: PaginationOptions },
    FileResource[],
    void,
    { Path: string[] }
  >
  GetFiles: RemoteFunction<
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
    void,
    {
      Files: string[]
    }
  >
  GetFileStar: RemoteFunction<{ FileId: string }, boolean, void, { Starred: boolean }>
  GetFileStars: RemoteFunction<
    { FileId?: string; UserId?: string; Pagination?: PaginationOptions },
    FileStarResource[],
    void,
    { FileStars: string[] }
  >
  GetFileNameValidationFlags: RemoteFunction<
    { Name: string; ParentId: string },
    FileNameValidationFlags,
    void,
    {
      Flags: FileNameValidationFlags
    }
  >
  GetNews: RemoteFunction<
    { Pagination?: PaginationOptions; AfterId?: string; Published?: boolean },
    string[],
    void,
    {
      NewsIds: string[]
    }
  >
  GetNewsEntry: RemoteFunction<{ NewsId: string }, NewsResource, void, { NewsEntry: string }>
  GetPasswordResetRequests: RemoteFunction<
    {
      Status?: PasswordResetRequestStatus
      Pagination?: PaginationOptions
    },
    PasswordResetRequestResource[],
    void,
    {
      Requests: string[]
    }
  >
  GetPasswordValidationFlags: RemoteFunction<
    {
      Password: string
      ConfirmPassword?: string
    },
    PasswordValidationFlags,
    void,
    {
      Flags: PasswordValidationFlags
    }
  >
  GetRootId: RemoteFunction<
    {
      UserId: string
    },
    string | undefined,
    void,
    {
      FileId?: string
    }
  >
  GetUser: RemoteFunction<
    {
      UserId: string
    },
    UserResource | undefined,
    void,
    {
      User?: string
    }
  >
  GetUsernameValidationFlags: RemoteFunction<
    { Username: string },
    UsernameValidationFlags,
    void,
    { Flags: UsernameValidationFlags }
  >
  GetUsers: RemoteFunction<
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
    void,
    { Users: string[] }
  >
  IsUserAdmin: RemoteFunction<{ UserId: string }, boolean, void, { IsAdmin: boolean }>
  MoveFile: RemoteFunction<{ FileId: string; NewParentId?: string; NewName?: string }, {}>
  RequestPasswordReset: RemoteFunction<{ UserId?: string; Username?: string }, {}>
  ResolveUsername: RemoteFunction<
    { Username: string },
    string | undefined,
    void,
    { UserId?: string }
  >
  SetFileAccess: RemoteFunction<
    { FileId: string; TargetUserId?: string; Level: FileAccessLevel },
    {}
  >
  SetFileStar: RemoteFunction<{ FileId: string; Starred: boolean }, {}>
  SetupRequirements: RemoteFunction<{}, { AdminSetupRequired: boolean }>
  SetUserRoles: RemoteFunction<{ UserId: string; Roles: UserRole[] }, {}>

  StreamClose: RemoteFunction<{ StreamId: string }, {}>
  StreamGetLength: RemoteFunction<{ StreamId: string }, number, void, { Length: number }>
  StreamGetPosition: RemoteFunction<{ StreamId: string }, number, void, { Position: number }>
  StreamOpen: RemoteFunction<
    { FileId: string; FileDataId: string; ForWriting: boolean },
    string,
    void,
    {
      StreamId: string
    }
  >
  StreamRead: RemoteFunction<
    { StreamId: string; Length: number },
    Uint8Array,
    void,
    { Data: Uint8Array }
  >
  StreamSetLength: RemoteFunction<{ StreamId: string; Length: number }, {}>
  StreamSetPosition: RemoteFunction<{ StreamId: string; Position: number }, {}>
  StreamWrite: RemoteFunction<{ StreamId: string; Data: Uint8Array | Blob }, {}>

  TranscribeAudio: RemoteFunction<
    { FileId: string; FileDataId?: string },
    {
      Text: string[]
      Status: AudioTranscriptionStatus
    }
  >

  TrashFile: RemoteFunction<{ FileId: string }, {}>
  UntrashFile: RemoteFunction<{ FileId: string }, {}>
  UpdateName: RemoteFunction<
    { FirstName: string; MiddleName?: string; LastName: string; DisplayName?: string },
    {}
  >
  UpdatePassword: RemoteFunction<
    { CurrentPassword: string; NewPassword: string; ConfirmPassword: string },
    {}
  >
  UpdateUsername: RemoteFunction<{ NewUsername: string }, {}>
  Me: RemoteFunction<{}, UserResource, void, { User: string }>
  GetNotifications: RemoteFunction<
    {
      ExcludeUnread: boolean
      ExcludeRead: boolean
      NotificationId?: string
      Pagination?: PaginationOptions
    },
    NotificationResource[],
    void,
    {
      Notifications: string[]
    }
  >
  ReadNotification: RemoteFunction<{ NotificationId: string }, {}>
  GetDiskUsage: RemoteFunction<{}, { DiskUsage: number; DiskTotal: number }>
  SetGoogleAuthentication: RemoteFunction<{ Token: string | null }, {}>
  GetGoogleAuthentication: RemoteFunction<
    {},
    GoogleAccountInformation | null,
    void,
    { Info: GoogleAccountInformation | null }
  >
  GetUserDiskUsage: RemoteFunction<
    { BaseDirectory?: string; UserId?: string },
    {
      FileCount: number
      DiskUsage: number
    }
  >
  GenerateFileToken: RemoteFunction<
    { FileId: string; FileDataId: string },
    string,
    void,
    { FileTokenId: string }
  >
}

export interface GoogleAccountInformation {
  Name: string
  Email: string
}

export const responseTranslators: TranslatorMap<ServerSideFunctions> = {
  AcceptPasswordResetRequest: [(data) => data, (data) => data],
  Agree: [(data) => data, (data) => data],
  AmIAdmin: [(data) => data, (data) => data.IsAdmin],
  AmILoggedIn: [(data) => data, (data) => data.IsLoggedIn],
  AuthenticateGoogle: [(data) => data, (data) => data],
  AuthenticatePassword: [(data) => data, (data) => data],
  AuthenticateToken: [(data) => data, (data) => data.RenewedToken],
  CreateAdmin: [(data) => data, (data) => data],
  FolderCreate: [(data) => data, (data) => JSON.parse(data.File) as FileResource],
  CreateNews: [(data) => data, (data) => JSON.parse(data.News) as NewsResource],
  CreateUser: [(data) => data, (data) => data],
  Deauthenticate: [(data) => data, (data) => data],
  DeclinePasswordResetRequest: [(data) => data, (data) => data],
  DeleteNews: [(data) => data, (data) => data],
  DidIAgree: [(data) => data, (data) => data.Agreed],
  FileCreate: [(data) => data, (data) => data.StreamId],
  FileDelete: [(data) => data, (data) => data],
  FileScan: [(data) => data, (data) => JSON.parse(data.Result) as VirusReportResource],
  FileGetMime: [(data) => data, (data) => data.MimeType],
  FileGetSize: [(data) => data, (data) => data.Size],
  GetFile: [(data) => data, ({ File }) => JSON.parse(File) as FileResource],
  GetFileAccesses: [
    (data) => data,
    (data) => data.FileAccesses.map((fileAccess) => JSON.parse(fileAccess) as FileAccessResource)
  ],
  GetFileAccessLevel: [(data) => data, (data) => data.Level],
  GetFileLogs: [
    (data) => data,
    (data) => data.FileLogs.map((fileLog) => JSON.parse(fileLog) as FileLogResource)
  ],
  GetFilePath: [
    (data) => data,
    (data) => data.Path.map((pathEntry) => JSON.parse(pathEntry) as FileResource)
  ],
  GetFiles: [(data) => data, ({ Files }) => Files.map((file) => JSON.parse(file) as FileResource)],
  GetFileStar: [(data) => data, (data) => data.Starred],
  GetFileStars: [
    (data) => data,
    ({ FileStars }) => FileStars.map((fileStar) => JSON.parse(fileStar) as FileStarResource)
  ],
  GetFileNameValidationFlags: [(data) => data, (data) => data.Flags],
  GetNews: [(data) => data, (data) => data.NewsIds],
  GetNewsEntry: [(data) => data, (data) => JSON.parse(data.NewsEntry) as NewsResource],
  GetPasswordResetRequests: [
    (data) => data,
    ({ Requests }) => Requests.map((request) => JSON.parse(request) as PasswordResetRequestResource)
  ],
  GetPasswordValidationFlags: [(data) => data, (data) => data.Flags],
  GetRootId: [(data) => data, (data) => data.FileId],
  GetUser: [
    (data) => data,
    ({ User }) => (User != null ? (JSON.parse(User) as UserResource) : undefined)
  ],
  GetUsernameValidationFlags: [(data) => data, (data) => data.Flags],
  GetUsers: [(data) => data, ({ Users }) => Users.map((user) => JSON.parse(user) as UserResource)],
  IsUserAdmin: [(data) => data, (data) => data.IsAdmin],
  MoveFile: [(data) => data, (data) => data],
  RequestPasswordReset: [(data) => data, (data) => data],
  ResolveUsername: [(data) => data, (data) => data.UserId],
  SetFileAccess: [(data) => data, (data) => data],
  SetFileStar: [(data) => data, (data) => data],
  SetupRequirements: [(data) => data, (data) => data],
  SetUserRoles: [(data) => data, (data) => data],

  StreamClose: [(data) => data, (data) => data],
  StreamGetLength: [(data) => data, (data) => data.Length],
  StreamGetPosition: [(data) => data, (data) => data.Position],
  StreamOpen: [(data) => data, (data) => data.StreamId],
  StreamRead: [(data) => data, (data) => data.Data],
  StreamSetLength: [(data) => data, (data) => data],
  StreamSetPosition: [(data) => data, (data) => data],
  StreamWrite: [(data) => data, (data) => data],

  TranscribeAudio: [(data) => data, (data) => data],

  TrashFile: [(data) => data, (data) => data],
  UntrashFile: [(data) => data, (data) => data],
  UpdateName: [(data) => data, (data) => data],
  UpdatePassword: [(data) => data, (data) => data],
  UpdateUsername: [(data) => data, (data) => data],
  Me: [(data) => data, ({ User }) => JSON.parse(User) as UserResource],
  FileGetDataEntries: [
    (data) => data,
    ({ FileDataEntries }) =>
      FileDataEntries.map((fileData) => JSON.parse(fileData) as FileDataResource)
  ],
  GetNotifications: [
    (data) => data,
    ({ Notifications }) =>
      Notifications.map((notification) => JSON.parse(notification) as NotificationResource)
  ],
  ReadNotification: [(data) => data, (data) => data],
  GetDiskUsage: [(data) => data, (data) => data],
  SetGoogleAuthentication: [(data) => data, (data) => data],
  GetGoogleAuthentication: [(data) => data, (data) => data.Info],
  GetUserDiskUsage: [(data) => data, (data) => data],
  GenerateFileToken: [(data) => data, (data) => data.FileTokenId]
}
