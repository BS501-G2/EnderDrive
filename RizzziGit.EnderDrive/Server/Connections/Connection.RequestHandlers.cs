namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private void RegisterHandlers()
  {
    RegisterRequestHandler(nameof(AmILoggedIn), AmILoggedIn);
    RegisterTransactedRequestHandler(nameof(SetupRequirements), SetupRequirements);
    RegisterTransactedRequestHandler(nameof(CreateAdmin), CreateAdmin);
    RegisterTransactedRequestHandler(nameof(ResolveUsername), ResolveUsername);
    RegisterTransactedRequestHandler(nameof(AuthenticatePassword), AuthenticatePassword);
    RegisterTransactedRequestHandler(nameof(AuthenticateGoogle), AuthenticateGoogle);
    RegisterTransactedRequestHandler(nameof(AuthenticateToken), AuthenticateToken);
    RegisterAuthenticatedRequestHandler(nameof(AmIAdmin), AmIAdmin);
    RegisterAuthenticatedRequestHandler(nameof(Me), Me);
    RegisterAuthenticatedRequestHandler(nameof(Deauthenticate), Deauthenticate);
    RegisterAuthenticatedRequestHandler(nameof(GetUser), GetUser);
    RegisterAuthenticatedRequestHandler(nameof(GetUsers), GetUsers);
    RegisterAuthenticatedRequestHandler(nameof(GetFiles), GetFiles);
    RegisterAuthenticatedRequestHandler(nameof(GetFileAccesses), GetFileAccesses);
    RegisterAuthenticatedRequestHandler(nameof(GetFileStars), GetFileStars);
    RegisterAuthenticatedRequestHandler(nameof(GetFileLogs), GetFileLogs);
    RegisterAuthenticatedRequestHandler(nameof(CreateNews), CreateNews, [UserRole.NewsEditor]);
    RegisterAuthenticatedRequestHandler(nameof(DeleteNews), DeleteNews, [UserRole.NewsEditor]);
    RegisterAuthenticatedRequestHandler(nameof(GetNews), GetNews);
    RegisterAuthenticatedRequestHandler(nameof(GetNewsEntry), GetNewsEntry);
    RegisterFileRequestHandler(nameof(SetFileStar), SetFileStar);
    RegisterFileRequestHandler(nameof(GetFileStar), GetFileStar);
    RegisterFileRequestHandler(nameof(GetFilePath), GetFilePath, FileAccessLevel.Read, null);
    RegisterFileRequestHandler(nameof(GetFile), GetFile, FileAccessLevel.Read, null);
    RegisterFileRequestHandler(nameof(FolderCreate), FolderCreate, FileAccessLevel.ReadWrite, null);
    RegisterAuthenticatedRequestHandler(nameof(DidIAgree), DidIAgree);
    RegisterAuthenticatedRequestHandler(nameof(Agree), Agree);
    RegisterFileRequestHandler(nameof(TrashFile), TrashFile, FileAccessLevel.ReadWrite, null);
    RegisterFileRequestHandler(nameof(UntrashFile), UntrashFile, FileAccessLevel.ReadWrite, null);
    RegisterFileRequestHandler(nameof(MoveFile), MoveFile, FileAccessLevel.ReadWrite, null);
    RegisterAdminRequestHandler(nameof(CreateUser), CreateUser);
    RegisterRequestHandler(nameof(GetUsernameValidationFlags), GetUsernameValidationFlags);
    RegisterRequestHandler(nameof(GetPasswordValidationFlags), GetPasswordValidationFlags);
    RegisterFileRequestHandler(nameof(SetFileAccess), SetFileAccess);
    RegisterAuthenticatedRequestHandler(nameof(GetFileAccessLevel), GetFileAccessLevel);
    RegisterTransactedRequestHandler(nameof(RequestPasswordReset), RequestPasswordReset);
    RegisterAdminRequestHandler(nameof(GetPasswordResetRequests), GetPasswordResetRequests);
    RegisterAdminRequestHandler(nameof(AcceptPasswordResetRequest), AcceptPasswordResetRequest);
    RegisterAdminRequestHandler(nameof(DeclinePasswordResetRequest), DeclinePasswordResetRequest);
    RegisterAdminRequestHandler(nameof(IsUserAdmin), IsUserAdmin);
    RegisterAdminRequestHandler(nameof(SetUserRoles), SetUserRoles);
    RegisterFileRequestHandler(
      nameof(TranscribeAudio),
      TranscribeAudio,
      FileAccessLevel.Read,
      FileType.File
    );
    RegisterAuthenticatedRequestHandler(nameof(UpdateUsername), UpdateUsername);
    RegisterAuthenticatedRequestHandler(nameof(UpdatePassword), UpdatePassword);
    RegisterAuthenticatedRequestHandler(nameof(GetRootId), GetRootId);
    RegisterFileRequestHandler(nameof(FileDelete), FileDelete);
    RegisterTransactedRequestHandler(
      nameof(GetFileNameValidationFlags),
      GetFileNameValidationFlags
    );
    RegisterAuthenticatedRequestHandler(nameof(UpdateName), UpdateName);
    RegisterFileRequestHandler(nameof(FileScan), FileScan, FileAccessLevel.Read, null);
    RegisterFileRequestHandler(
      nameof(FileGetMime),
      FileGetMime,
      FileAccessLevel.Read,
      FileType.File
    );
    RegisterFileRequestHandler(
      nameof(FileGetDataEntries),
      FileGetDataEntries,
      FileAccessLevel.Read,
      FileType.File
    );
    RegisterFileRequestHandler(
      nameof(FileGetSize),
      FileGetSize,
      FileAccessLevel.Read,
      FileType.File
    );
    RegisterFileRequestHandler(nameof(FileCreate), FileCreate);
    RegisterStreamRequestHandler(nameof(StreamWrite), StreamWrite);
    RegisterStreamRequestHandler(nameof(StreamRead), StreamRead);
    RegisterStreamRequestHandler(nameof(StreamGetLength), StreamGetLength);
    RegisterStreamRequestHandler(nameof(StreamSetLength), StreamSetLength);
    RegisterStreamRequestHandler(nameof(StreamSetPosition), StreamSetPosition);
    RegisterStreamRequestHandler(nameof(StreamGetPosition), StreamGetPosition);
    RegisterStreamRequestHandler(nameof(StreamClose), StreamClose);
    RegisterFileRequestHandler(nameof(StreamOpen), StreamOpen, FileAccessLevel.Read, FileType.File);
  }
}
