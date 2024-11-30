export interface Resource {
  Id: string
}

export interface FileResource extends Resource {
  ParentId?: string
  OwnerUserId: string
  Name: string
  Type: FileType
  TrashTime: string
}

export enum FileType {
  File,
  Folder
}

export enum TrashOptions {
  NonInclusive,
  Inclusive,
  Exclusive
}

export enum FileNameValidationFlags {
  OK = 0,

  TooLong = 1 << 0,
  TooShort = 1 << 1,
  InvalidCharacters = 1 << 2,
  FileExists = 1 << 3
}

export interface NewsResource extends Resource {
  Title: string

  PublishTime?: number
  AuthorUserId?: string
  Image?: Uint8Array
}

export interface VirusReportResource extends Resource {
  FileId: string
  FileDataId: string
  Status: VirusReportStatus
  Viruses?: string[]
}

export enum VirusReportStatus {
  Pending,
  Failed,
  Completed
}

export interface FileAccessResource extends Resource {
  FileId: string
  AuthorUserId: string
  TargetUserId?: string
  Level: FileAccessLevel
}

export enum FileAccessLevel {
  None,
  Read,
  ReadWrite,
  Manage,
  Full
}

export enum FileAccessTargetEntityType {
  User,
  Group
}

export interface FileLogResource extends Resource {
  Type: FileLogType
  ActorUserId: string
  FileId: string
  FileDataId?: string
  CreateTime: number
}

export enum FileLogType {
  Create,
  Update,
  Trash,
  Untrash,
  Share,
  Unshare,
  Delete,
  Read
}

export interface FileStarResource extends Resource {
  FileId: string
  UserId: string
  CreateTime: number
  Starred: boolean
}

export interface PasswordResetRequestResource extends Resource {
  UserId: string

  Status: PasswordResetRequestStatus
}

export enum PasswordResetRequestStatus {
  Pending,
  Accepted,
  Declined
}

export enum PasswordValidationFlags {
  OK = 0,
  TooShort = 1 << 0,
  TooLong = 1 << 1,
  NoRequiredChars = 1 << 2,
  PasswordMismatch = 1 << 3
}

export interface UserResource extends Resource {
  Username: string
  FirstName: string
  MiddleName?: string
  LastName: string
  DisplayName?: string
  Roles: UserRole[]
}

export enum UserRole {
  None = 0b01,
  NewsEditor = 0b10
}

export enum UsernameValidationFlags {
  OK = 0,
  TooShort = 1 << 0,
  TooLong = 1 << 1,
  InvalidChars = 1 << 2
}

export enum AudioTranscriptionStatus {
  NotRunning,
  Pending,
  Running,
  Done,
  Error
}

export interface FileDataResource extends Resource {
  CreateTime: number
  FileId: string
  AuthorUserId?: string
  BaseFileDataId?: string
}
