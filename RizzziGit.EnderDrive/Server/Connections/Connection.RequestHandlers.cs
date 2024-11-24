namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private void RegisterHandlers(ConnectionContext context)
  {
    void registerHandler<S, R>(ServerSideRequestCode code, RequestHandler<S, R> handler) =>
      RegisterHandler<S, R>(context, code, handler);

    void registerTransactedHandler<S, R>(
      ServerSideRequestCode code,
      TransactedRequestHandler<S, R> handler
    ) => RegisterTransactedHandler<S, R>(context, code, handler);

    void registerAuthenticatedHandler<S, R>(
      ServerSideRequestCode code,
      AuthenticatedRequestHandler<S, R> handler,
      UserRole[]? requiredIncludeRole = null,
      UserRole[]? requiredExcludeRole = null
    ) =>
      RegisterAuthenticatedHandler(
        context,
        code,
        handler,
        requiredIncludeRole,
        requiredExcludeRole
      );

    void registerAdminHandler<S, R>(
      ServerSideRequestCode code,
      AdminRequestHandler<S, R> handler,
      UserRole[]? requiredIncludeRole = null,
      UserRole[]? requiredExcludeRole = null
    ) =>
      RegisterAdminHandler<S, R>(context, code, handler, requiredIncludeRole, requiredExcludeRole);

    void registerFileHandler<S, R>(
      ServerSideRequestCode code,
      FileRequestHandler<S, R> handler,
      FileType? fileType = null,
      FileAccessLevel? fileAccessLevel = null
    )
      where S : BaseFileRequest =>
      RegisterFileHandler(context, code, handler, fileAccessLevel, fileType);

    registerHandler(ServerSideRequestCode.AmILoggedIn, AmILoggedIn);
    registerTransactedHandler(ServerSideRequestCode.SetupRequirements, SetupRequirements);
    registerTransactedHandler(ServerSideRequestCode.CreateAdmin, CreateAdmin);
    registerTransactedHandler(ServerSideRequestCode.ResolveUsername, ResolveUsername);
    registerTransactedHandler(ServerSideRequestCode.AuthenticatePassword, AuthenticatePassword);
    registerTransactedHandler(ServerSideRequestCode.AuthenticateGoogle, AuthenticateGoogle);
    registerTransactedHandler(ServerSideRequestCode.AuthenticateToken, AuthenticateToken);
    registerAuthenticatedHandler(ServerSideRequestCode.AmIAdmin, AmIAdmin);
    registerAuthenticatedHandler(ServerSideRequestCode.Me, Me);
    registerAuthenticatedHandler(ServerSideRequestCode.Deauthenticate, Deauthenticate);
    registerAuthenticatedHandler(ServerSideRequestCode.GetUser, GetUser);
    registerAuthenticatedHandler(ServerSideRequestCode.GetUsers, GetUsers);
    registerAuthenticatedHandler(ServerSideRequestCode.GetFiles, GetFiles);
    registerAuthenticatedHandler(ServerSideRequestCode.GetFileAccesses, GetFileAccesses);
    registerAuthenticatedHandler(ServerSideRequestCode.GetFileStars, GetFileStars);
    registerAuthenticatedHandler(ServerSideRequestCode.GetFileLogs, GetFileLogs);
    registerFileHandler(ServerSideRequestCode.CreateFile, CreateFile);
    registerAuthenticatedHandler(ServerSideRequestCode.OpenStream, OpenStream);
    registerAuthenticatedHandler(
      ServerSideRequestCode.CreateNews,
      CreateNews,
      [UserRole.NewsEditor]
    );
    registerAuthenticatedHandler(
      ServerSideRequestCode.DeleteNews,
      DeleteNews,
      [UserRole.NewsEditor]
    );
    registerAuthenticatedHandler(ServerSideRequestCode.GetNews, GetNews);
    registerAuthenticatedHandler(ServerSideRequestCode.GetNewsEntry, GetNewsEntry);
    registerFileHandler(ServerSideRequestCode.SetFileStar, SetFileStar);
    registerFileHandler(ServerSideRequestCode.GetFileStar, GetFileStar);
    registerFileHandler(ServerSideRequestCode.GetFilePath, GetFilePath, null, FileAccessLevel.Read);
    registerFileHandler(ServerSideRequestCode.GetFile, GetFile, null, FileAccessLevel.Read);
    registerFileHandler(
      ServerSideRequestCode.CreateFolder,
      CreateFolder,
      null,
      FileAccessLevel.ReadWrite
    );
    registerFileHandler(ServerSideRequestCode.GetFileMime, GetFileMime, null, FileAccessLevel.Read);
    registerFileHandler(ServerSideRequestCode.GetFileSize, GetFileSize, null, FileAccessLevel.Read);
    registerAuthenticatedHandler(ServerSideRequestCode.GetFileContents, GetFileContents, null);
    registerAuthenticatedHandler(ServerSideRequestCode.GetFileSnapshots, GetFileSnapshots, null);
    registerFileHandler(ServerSideRequestCode.ScanFile, ScanFile, null, FileAccessLevel.Read);
    registerFileHandler(
      ServerSideRequestCode.GetMainFileContent,
      GetMainFileContent,
      null,
      FileAccessLevel.Read
    );
    registerFileHandler(
      ServerSideRequestCode.GetLatestFileSnapshot,
      GetLatestFileSnapshot,
      null,
      FileAccessLevel.Read
    );
    registerFileHandler(
      ServerSideRequestCode.GetOldestFileSnapshot,
      GetOldestFileSnapshot,
      null,
      FileAccessLevel.Read
    );
    registerHandler(ServerSideRequestCode.CloseStream, CloseStream);
    registerHandler(ServerSideRequestCode.ReadStream, ReadStream);
    registerHandler(ServerSideRequestCode.WriteStream, WriteStream);
    registerHandler(ServerSideRequestCode.SetPosition, SetPosition);
    registerHandler(ServerSideRequestCode.GetStreamSize, GetStreamSize);
    registerHandler(ServerSideRequestCode.GetStreamPosition, GetStreamPosition);
    registerAuthenticatedHandler(ServerSideRequestCode.DidIAgree, DidIAgree);
    registerAuthenticatedHandler(ServerSideRequestCode.Agree, Agree);
    registerFileHandler(
      ServerSideRequestCode.TrashFile,
      TrashFile,
      null,
      FileAccessLevel.ReadWrite
    );
    registerFileHandler(
      ServerSideRequestCode.UntrashFile,
      UntrashFile,
      null,
      FileAccessLevel.ReadWrite
    );
    registerFileHandler(ServerSideRequestCode.MoveFile, MoveFile, null, FileAccessLevel.ReadWrite);
    registerAdminHandler(ServerSideRequestCode.CreateUser, CreateUser);
    registerHandler(ServerSideRequestCode.GetUsernameValidationFlags, GetUsernameValidationFlags);
    registerHandler(ServerSideRequestCode.GetPasswordValidationFlags, GetPasswordValidationFlags);
    registerFileHandler(ServerSideRequestCode.SetFileAccess, SetFileAccess);
    registerAuthenticatedHandler(ServerSideRequestCode.GetFileAccessLevel, GetFileAccessLevel);
    registerTransactedHandler(ServerSideRequestCode.RequestPasswordReset, RequestPasswordReset);
    registerAdminHandler(ServerSideRequestCode.GetPasswordResetRequests, GetPasswordResetRequests);
    registerAdminHandler(
      ServerSideRequestCode.AcceptPasswordResetRequest,
      AcceptPasswordResetRequest
    );
    registerAdminHandler(
      ServerSideRequestCode.DeclinePasswordResetRequest,
      DeclinePasswordResetRequest
    );
    registerAdminHandler(ServerSideRequestCode.IsUserAdmin, IsUserAdmin);
    registerAdminHandler(ServerSideRequestCode.SetUserRoles, SetUserRoles);
    registerFileHandler(
      ServerSideRequestCode.TranscribeAudio,
      TranscribeAudio,
      FileType.File,
      FileAccessLevel.Read
    );
    registerAuthenticatedHandler(ServerSideRequestCode.UpdateUsername, UpdateUsername);
    registerAuthenticatedHandler(ServerSideRequestCode.UpdatePassword, UpdatePassword);
    registerAdminHandler(ServerSideRequestCode.GetRootId, GetRootId);
    registerHandler(ServerSideRequestCode.TruncateStream, TruncateStream);
    registerFileHandler(ServerSideRequestCode.DeleteFile, DeleteFile);
    registerTransactedHandler(ServerSideRequestCode.GetFileNameValidationFlags, GetFileNameValidationFlags);
    registerAuthenticatedHandler(ServerSideRequestCode.UpdateName, UpdateName);
  }
}

public enum ServerSideRequestCode : byte
{
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

  UpdateUsername,
  UpdatePassword,

  GetRootId,
  TruncateStream,

  DeleteFile,
  GetFileNameValidationFlags,
  GetNewsEntry,
  UpdateName
}
