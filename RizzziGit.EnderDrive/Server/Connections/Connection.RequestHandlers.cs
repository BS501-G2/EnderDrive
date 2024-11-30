namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
  private void RegisterHandlers()
  {
    RegisterHandler(nameof(AmILoggedIn), AmILoggedIn);
    RegisterTransactedHandler(nameof(SetupRequirements), SetupRequirements);
    RegisterTransactedHandler(nameof(CreateAdmin), CreateAdmin);
    RegisterTransactedHandler(nameof(ResolveUsername), ResolveUsername);
    RegisterTransactedHandler(nameof(AuthenticatePassword), AuthenticatePassword);
    RegisterTransactedHandler(nameof(AuthenticateGoogle), AuthenticateGoogle);
    RegisterTransactedHandler(nameof(AuthenticateToken), AuthenticateToken);
    RegisterAuthenticatedHandler(nameof(AmIAdmin), AmIAdmin);
    RegisterAuthenticatedHandler(nameof(Me), Me);
    RegisterAuthenticatedHandler(nameof(Deauthenticate), Deauthenticate);
    RegisterAuthenticatedHandler(nameof(GetUser), GetUser);
    RegisterAuthenticatedHandler(nameof(GetUser), GetUsers);
    RegisterAuthenticatedHandler(nameof(GetFiles), GetFiles);
    RegisterAuthenticatedHandler(nameof(GetFileAccesses), GetFileAccesses);
    RegisterAuthenticatedHandler(nameof(GetFileStars), GetFileStars);
    RegisterAuthenticatedHandler(nameof(CreateNews), CreateNews, [UserRole.NewsEditor]);
    RegisterAuthenticatedHandler(nameof(DeleteNews), DeleteNews, [UserRole.NewsEditor]);
    RegisterAuthenticatedHandler(nameof(GetNews), GetNews);
    RegisterAuthenticatedHandler(nameof(GetNewsEntry), GetNewsEntry);
    RegisterFileHandler(nameof(SetFileStar), SetFileStar);
    RegisterFileHandler(nameof(GetFileStar), GetFileStar);
    RegisterFileHandler(nameof(GetFilePath), GetFilePath, FileAccessLevel.Read, null);
    RegisterFileHandler(nameof(GetFile), GetFile, FileAccessLevel.Read, null);
    RegisterFileHandler(nameof(FolderCreate), FolderCreate, FileAccessLevel.ReadWrite, null);
    RegisterAuthenticatedHandler(nameof(DidIAgree), DidIAgree);
    RegisterAuthenticatedHandler(nameof(Agree), Agree);
    RegisterFileHandler(nameof(TrashFile), TrashFile, FileAccessLevel.ReadWrite, null);
    RegisterFileHandler(nameof(UntrashFile), UntrashFile, FileAccessLevel.ReadWrite, null);
    RegisterFileHandler(nameof(MoveFile), MoveFile, FileAccessLevel.ReadWrite, null);
    RegisterAdminHandler(nameof(CreateUser), CreateUser);
    RegisterHandler(nameof(GetUsernameValidationFlags), GetUsernameValidationFlags);
    RegisterHandler(nameof(GetPasswordValidationFlags), GetPasswordValidationFlags);
    RegisterFileHandler(nameof(SetFileAccess), SetFileAccess);
    RegisterAuthenticatedHandler(nameof(GetFileAccessLevel), GetFileAccessLevel);
    RegisterTransactedHandler(nameof(RequestPasswordReset), RequestPasswordReset);
    RegisterAdminHandler(nameof(GetPasswordResetRequests), GetPasswordResetRequests);
    RegisterAdminHandler(nameof(AcceptPasswordResetRequest), AcceptPasswordResetRequest);
    RegisterAdminHandler(nameof(DeclinePasswordResetRequest), DeclinePasswordResetRequest);
    RegisterAdminHandler(nameof(IsUserAdmin), IsUserAdmin);
    RegisterAdminHandler(nameof(SetUserRoles), SetUserRoles);
    RegisterFileHandler(
      nameof(TranscribeAudio),
      TranscribeAudio,
      FileAccessLevel.Read,
      FileType.File
    );
    RegisterAuthenticatedHandler(nameof(UpdateUsername), UpdateUsername);
    RegisterAuthenticatedHandler(nameof(UpdatePassword), UpdatePassword);
    RegisterAdminHandler(nameof(GetRootId), GetRootId);
    RegisterFileHandler(nameof(FileDelete), FileDelete);
    RegisterTransactedHandler(nameof(GetFileNameValidationFlags), GetFileNameValidationFlags);
    RegisterAuthenticatedHandler(nameof(UpdateName), UpdateName);
    RegisterFileHandler(nameof(FileScan), FileScan, FileAccessLevel.Read, null);
    RegisterFileHandler(nameof(FileGetMime), FileGetMime, FileAccessLevel.Read, FileType.File);
    RegisterFileHandler(
      nameof(FileGetDataEntries),
      FileGetDataEntries,
      FileAccessLevel.Read,
      FileType.File
    );
    RegisterFileHandler(
      nameof(FileDataGetSize),
      FileDataGetSize,
      FileAccessLevel.Read,
      FileType.File
    );
    RegisterFileHandler(nameof(FileCreate), FileCreate);
    RegisterStreamHandler(nameof(StreamWrite), StreamWrite);
    RegisterStreamHandler(nameof(StreamRead), StreamRead);
    RegisterStreamHandler(nameof(StreamGetLength), StreamGetLength);
    RegisterStreamHandler(nameof(StreamSetLength), StreamSetLength);
    RegisterStreamHandler(nameof(StreamSetPosition), StreamSetPosition);
    RegisterStreamHandler(nameof(StreamGetPosition), StreamGetPosition);
    RegisterStreamHandler(nameof(StreamClose), StreamClose);
    RegisterFileHandler(nameof(StreamOpen), StreamOpen, FileAccessLevel.Read, FileType.File);
  }
}
