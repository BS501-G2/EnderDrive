using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderDrive.Server.Services;

using Commons.Memory;
using Resources;
using Utilities;

public sealed partial class Connection
{
    private UnlockedUserAuthentication Internal_EnsureAuthentication()
    {
        UnlockedUserAuthentication unlockedUserAuthentication =
            GetContext().CurrentUser ?? throw new InvalidOperationException("Not authenticated.");

        return unlockedUserAuthentication;
    }

    private async Task<User> Internal_Me(
        ResourceTransaction transaction,
        UserAuthentication userAuthentication
    )
    {
        User user = await Resources
            .GetUsers(transaction, id: userAuthentication.UserId)
            .ToAsyncEnumerable()
            .FirstAsync(transaction.CancellationToken);

        return user;
    }

    private static T Internal_EnsureExists<T>(T? item)
        where T : ResourceData
    {
        if (item == null)
        {
            throw new InvalidOperationException($"Resource item not found");
        }

        return item;
    }

    private async Task<File> Internal_GetFile(
        ResourceTransaction transaction,
        User me,
        ObjectId? objectId
    )
    {
        return Internal_EnsureExists(
            objectId != null
                ? await Resources
                    .GetFiles(transaction, id: objectId)
                    .ToAsyncEnumerable()
                    .FirstOrDefaultAsync(transaction.CancellationToken)
                : await Resources.GetRootFolder(transaction, me)
        );
    }

    private static ObjectId? Internal_ParseId(string? objectId)
    {
        if (objectId == null)
        {
            return null;
        }

        return ObjectId.Parse(objectId);
    }

    private sealed record class SetupRequirementsRequest { };

    private sealed record class SetupRequirementsResponse
    {
        [BsonElement("adminSetupRequired")]
        public required bool AdminSetupRequired;
    };

    private RequestHandler<SetupRequirementsRequest, SetupRequirementsResponse> SetupRequirements =>
        async (transaction, request) =>
            new()
            {
                AdminSetupRequired = !await Resources
                    .GetAdminAccesses(transaction)
                    .ToAsyncEnumerable()
                    .AnyAsync(transaction.CancellationToken)
            };

    private sealed record class CreateAdminRequest()
    {
        [BsonElement("username")]
        public required string Username;

        [BsonElement("password")]
        public required string Password;

        [BsonElement("confirmPassword")]
        public required string ConfirmPassword;

        [BsonElement("lastName")]
        public required string LastName;

        [BsonElement("firstName")]
        public required string FirstName;

        [BsonElement("middleName")]
        public required string? MiddleName;

        [BsonElement("displayName")]
        public required string? DisplayName;
    };

    private sealed record class CreateAdminResponse() { };

    private RequestHandler<CreateAdminRequest, CreateAdminResponse> CreateAdmin =>
        async (transaction, request) =>
        {
            if (
                await Resources
                    .GetAdminAccesses(transaction)
                    .ToAsyncEnumerable()
                    .AnyAsync(transaction.CancellationToken)
            )
            {
                throw new InvalidOperationException("Admin user already exists.");
            }

            UsernameValidation usernameValidation = Resources.ValidateUsername(request.Username);

            if (usernameValidation != UsernameValidation.OK)
            {
                throw new InvalidOperationException($"Invalid Username: {usernameValidation}");
            }

            PasswordVerification passwordVerification = Resources.VerifyPassword(request.Password);

            if (passwordVerification != PasswordVerification.OK)
            {
                throw new InvalidOperationException($"Invalid Password: {passwordVerification}");
            }

            await Resources.CreateUser(
                transaction,
                request.Username,
                request.Username,
                request.MiddleName,
                request.LastName,
                request.DisplayName,
                request.Password
            );

            return new();
        };

    private sealed record class ResolveUsernameRequest
    {
        [BsonElement("username")]
        public required string Username;
    }

    private sealed record class ResolveUsernameResponse
    {
        [BsonElement("userId")]
        public required ObjectId? UserId;
    }

    private RequestHandler<ResolveUsernameRequest, ResolveUsernameResponse> ResolveUsername =>
        async (transaction, request) =>
            new()
            {
                UserId = (
                    await Resources
                        .GetUsers(transaction, request.Username)
                        .ToAsyncEnumerable()
                        .FirstOrDefaultAsync(transaction.CancellationToken)
                )?.Id
            };

    private sealed record class AuthenticatePasswordRequest
    {
        [BsonElement("userId")]
        public required ObjectId UserId;

        [BsonElement("password")]
        public required string Password;
    }

    private sealed record class AuthenticatePasswordResponse
    {
        [BsonElement("userId")]
        public required ObjectId UserId;

        [BsonElement("token")]
        public required string Token;
    }

    private RequestHandler<
        AuthenticatePasswordRequest,
        AuthenticatePasswordResponse
    > AuthenticatePassword =>
        async (transaction, request) =>
        {
            if (GetContext().CurrentUser != null)
            {
                throw new InvalidOperationException("Already signed in.");
            }

            User user =
                await Resources
                    .GetUsers(transaction, id: request.UserId)
                    .ToAsyncEnumerable()
                    .FirstOrDefaultAsync(transaction.CancellationToken)
                ?? throw new InvalidOperationException("Invalid username or password.");

            UserAuthentication userAuthentication =
                await Resources
                    .GetUserAuthentications(transaction, user, UserAuthenticationType.Password)
                    .ToAsyncEnumerable()
                    .FirstOrDefaultAsync(transaction.CancellationToken)
                ?? throw new InvalidOperationException("Password is unavailable for this account.");

            UnlockedUserAuthentication unlockedUserAuthentication;
            try
            {
                unlockedUserAuthentication = userAuthentication.Unlock(request.Password);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Invalid username or password.", exception);
            }

            await Resources.TruncateLatestToken(transaction, user, 10);
            CompositeBuffer tokenPayload = CompositeBuffer.From(
                CompositeBuffer.Random(16).ToHexString()
            );

            GetContext().CurrentUser = await Resources.AddUserAuthentication(
                transaction,
                user,
                unlockedUserAuthentication,
                UserAuthenticationType.Token,
                tokenPayload.ToByteArray()
            );

            return new() { UserId = user.Id, Token = tokenPayload.ToString() };
        };

    private sealed record class AuthenticateGoogleRequest
    {
        [BsonElement("token")]
        public required string Token;
    }

    private sealed record class AuthenticateGoogleResponse
    {
        [BsonElement("userId")]
        public required string UserId;

        [BsonElement("token")]
        public required string Token;
    }

    private RequestHandler<
        AuthenticateGoogleRequest,
        AuthenticateGoogleResponse
    > AuthenticateGoogle =>
        async (transaction, request) =>
        {
            byte[] payload = await Server.GoogleService.GetPayload(
                request.Token,
                transaction.CancellationToken
            );

            UnlockedUserAuthentication? unlockedUserAuthentication = null;
            await foreach (
                UserAuthentication userAuthentication in Resources
                    .GetUserAuthentications(transaction, type: UserAuthenticationType.Google)
                    .ToAsyncEnumerable()
            )
            {
                try
                {
                    unlockedUserAuthentication = userAuthentication.Unlock(payload);
                    break;
                }
                catch { }
            }

            if (unlockedUserAuthentication == null)
            {
                throw new InvalidOperationException(
                    "No EnderDrive account associated with this Google account."
                );
            }

            User user = await Resources
                .GetUsers(transaction, id: unlockedUserAuthentication.UserId)
                .ToAsyncEnumerable()
                .FirstAsync(transaction.CancellationToken);

            await Resources.TruncateLatestToken(transaction, user, 10);
            CompositeBuffer tokenPayload = CompositeBuffer.From(
                CompositeBuffer.Random(16).ToHexString()
            );

            GetContext().CurrentUser = await Resources.AddUserAuthentication(
                transaction,
                user,
                unlockedUserAuthentication,
                UserAuthenticationType.Token,
                tokenPayload.ToByteArray()
            );

            return new() { UserId = user.Id.ToString(), Token = tokenPayload.ToString() };
        };

    private sealed record class AuthenticateTokenRequest
    {
        [BsonElement("userId")]
        public required ObjectId UserId;

        [BsonElement("token")]
        public required string Token;
    };

    private sealed record class AuthenticateTokenResponse
    {
        [BsonElement("renewedToken")]
        public required string? RenewedToken;
    };

    private RequestHandler<AuthenticateTokenRequest, AuthenticateTokenResponse> AuthenticateToken =>
        async (transaction, request) =>
        {
            if (GetContext().CurrentUser != null)
            {
                throw new InvalidOperationException("Already signed in.");
            }

            User? user =
                await Resources
                    .GetUsers(transaction, id: request.UserId)
                    .ToAsyncEnumerable()
                    .FirstOrDefaultAsync(transaction.CancellationToken)
                ?? throw new InvalidOperationException("Invalid user id.");

            UnlockedUserAuthentication? unlockedUserAuthentication = null;
            await foreach (
                UserAuthentication userAuthentication in Resources
                    .GetUserAuthentications(
                        transaction,
                        user: user,
                        type: UserAuthenticationType.Token
                    )
                    .ToAsyncEnumerable()
            )
            {
                try
                {
                    unlockedUserAuthentication = userAuthentication.Unlock(request.Token);
                    break;
                }
                catch { }
            }

            if (unlockedUserAuthentication == null)
            {
                throw new InvalidOperationException("Invalid token.");
            }

            GetContext().CurrentUser = unlockedUserAuthentication;

            return new() { RenewedToken = null };
        };

    private sealed record class WhoAmIRequest() { };

    private sealed record class WhoAmIResponse
    {
        [BsonElement("userId")]
        public required string? UserId;
    }

    private RequestHandler<WhoAmIRequest, WhoAmIResponse> WhoAmI =>
        (transaction, request) =>
        {
            return Task.FromResult<WhoAmIResponse>(
                new() { UserId = GetContext().CurrentUser?.UserId.ToString() }
            );
        };

    private sealed record class DeauthenticateRequest { }

    private sealed record class DeauthenticateResponse { }

    private RequestHandler<DeauthenticateRequest, DeauthenticateResponse> Deauthenticate =>
        (transaction, request) =>
        {
            GetContext().CurrentUser = null;

            return Task.FromResult<DeauthenticateResponse>(new());
        };

    private sealed record class GetUserRequest
    {
        [BsonElement("userId")]
        public required ObjectId UserId;
    };

    private sealed record class GetUserResponse
    {
        [BsonElement("user")]
        public required string? User;
    };

    private RequestHandler<GetUserRequest, GetUserResponse> GetUser =>
        async (transaction, request) =>
        {
            User? user = await Resources
                .GetUsers(transaction, id: request.UserId)
                .ToAsyncEnumerable()
                .FirstOrDefaultAsync(transaction.CancellationToken);

            return new() { User = user != null ? JToken.FromObject(user).ToString() : null };
        };

    private sealed record class GetUsersRequest
    {
        [BsonElement("searchString")]
        public required string? SearchString;

        [BsonElement("minRole")]
        public required UserRole? MinRole;

        [BsonElement("maxRole")]
        public required UserRole? MaxRole;

        [BsonElement("username")]
        public required string? Username;

        [BsonElement("id")]
        public required ObjectId? Id;

        [BsonElement("pagination")]
        public required PaginationOptions? Pagination;
    }

    private sealed record class GetUsersResponse
    {
        [BsonElement("users")]
        public required string[] Users;
    }

    private RequestHandler<GetUsersRequest, GetUsersResponse> GetUsers =>
        async (transaction, request) =>
        {
            User[] users = await Resources
                .GetUsers(
                    transaction,
                    request.SearchString,
                    request.MinRole,
                    request.MaxRole,
                    request.Username,
                    request.Id
                )
                .ApplyPagination(request.Pagination)
                .ToAsyncEnumerable()
                .ToArrayAsync(transaction.CancellationToken);

            return new()
            {
                Users = users.Select((user) => JToken.FromObject(user).ToString()).ToArray()
            };
        };

    private sealed record class GetFileRequest
    {
        [BsonElement("fileId")]
        public required string? FileId;
    }

    private sealed record class GetFileResponse
    {
        [BsonElement("file")]
        public required string File;
    }

    private RequestHandler<GetFileRequest, GetFileResponse> GetFile =>
        async (transaction, request) =>
        {
            UnlockedUserAuthentication userAuthentication = Internal_EnsureAuthentication();
            User me = await Internal_Me(transaction, userAuthentication);
            File file = await Internal_GetFile(transaction, me, Internal_ParseId(request.FileId));

            FileAccessResult result =
                await Resources.FindFileAccess(transaction, file, me, userAuthentication)
                ?? throw new InvalidOperationException("No access to this file");

            return new() { File = JToken.FromObject(file).ToString() };
        };

    private sealed record class GetFilesRequest
    {
        [BsonElement("parentFolderId")]
        public required ObjectId? ParentFolderId;

        [BsonElement("fileType")]
        public required FileType? FileType;

        [BsonElement("name")]
        public required string? Name;

        [BsonElement("ownerUserId")]
        public required ObjectId? OwnerUserId;

        [BsonElement("id")]
        public required ObjectId? Id;

        [BsonElement("trashOptions")]
        public required TrashOptions? TrashOptions;

        [BsonElement("pagination")]
        public required PaginationOptions? Pagination;
    }

    private sealed record class GetFilesResponse
    {
        [BsonElement("files")]
        public required string[] Files;
    }

    private RequestHandler<GetFilesRequest, GetFilesResponse> GetFiles =>
        async (transaction, request) =>
        {
            UnlockedUserAuthentication userAuthentication = Internal_EnsureAuthentication();
            User me = await Internal_Me(transaction, userAuthentication);

            File? parentFolder = await Internal_GetFile(transaction, me, request.ParentFolderId);
            if (parentFolder != null)
            {
                FileAccessResult result =
                    await Resources.FindFileAccess(
                        transaction,
                        parentFolder,
                        me,
                        userAuthentication
                    ) ?? throw new InvalidOperationException("No access to this file");
            }

            User? ownerUser =
                request.OwnerUserId != null
                    ? await Resources
                        .GetUsers(transaction, id: request.OwnerUserId)
                        .ToAsyncEnumerable()
                        .FirstOrDefaultAsync(transaction.CancellationToken)
                    : null;

            if (
                ownerUser == null
                || (
                    ownerUser.Id != me.Id
                    && !await Resources
                        .GetAdminAccesses(transaction, userId: me.Id)
                        .ToAsyncEnumerable()
                        .AnyAsync(transaction.CancellationToken)
                )
            )
            {
                throw new InvalidOperationException(
                    "Owner user is not required is required for non-admin users"
                );
            }

            File[] files = await Resources
                .GetFiles(
                    transaction,
                    parentFolder,
                    request.FileType,
                    request.Name,
                    ownerUser,
                    request.Id,
                    request.TrashOptions ?? TrashOptions.NotIncluded
                )
                .ApplyPagination(request.Pagination)
                .ToAsyncEnumerable()
                .ToArrayAsync(transaction.CancellationToken);

            return new()
            {
                Files = files.Select((file) => JToken.FromObject(file).ToString()).ToArray()
            };
        };

    private sealed record class GetFileAccessesRequest
    {
        [BsonElement("targetUserId")]
        public required ObjectId? TargetUserId;

        [BsonElement("targetFileId")]
        public required ObjectId? TargetFileId;

        [BsonElement("authorUserId")]
        public required ObjectId? AuthorUserId;

        [BsonElement("level")]
        public required FileAccessLevel? Level;

        [BsonElement("pagination")]
        public required PaginationOptions? Pagination;
    };

    private sealed record class GetFileAccessesResponse
    {
        [BsonElement("fileAccesses")]
        public required string[] FileAccesses;
    };

    private RequestHandler<GetFileAccessesRequest, GetFileAccessesResponse> GetFileAccesses =>
        async (transaction, request) =>
        {
            UnlockedUserAuthentication userAuthentication = Internal_EnsureAuthentication();
            User me = await Internal_Me(transaction, userAuthentication);

            User? targetUser = null;
            Group? targetGroup = null;
            File? targetFile = null;
            User? authorUser = null;

            if (request.TargetFileId != null)
            {
                targetFile = await Internal_GetFile(transaction, me, request.TargetFileId);
                FileAccessResult? result = await Resources.FindFileAccess(
                    transaction,
                    targetFile,
                    me,
                    userAuthentication,
                    FileAccessLevel.Manage
                );

                if (
                    result == null
                    && !await Resources
                        .GetAdminAccesses(transaction, userId: me.Id)
                        .ToAsyncEnumerable()
                        .AnyAsync(transaction.CancellationToken)
                )
                {
                    throw new InvalidOperationException("No access to this file");
                }
            }

            if (request.TargetUserId != null)
            {
                if (
                    targetFile == null
                    && request.TargetUserId != me.Id
                    && !await Resources
                        .GetAdminAccesses(transaction, userId: me.Id)
                        .ToAsyncEnumerable()
                        .AnyAsync(transaction.CancellationToken)
                )
                {
                    throw new InvalidOperationException(
                        "Target file must be set if the target user id is not yourself."
                    );
                }

                targetUser = Internal_EnsureExists(
                    await Resources
                        .GetUsers(transaction, id: request.TargetUserId)
                        .ToAsyncEnumerable()
                        .FirstOrDefaultAsync(transaction.CancellationToken)
                );
            }

            if (request.AuthorUserId != null)
            {
                if (
                    targetUser == null
                    && !await Resources
                        .GetAdminAccesses(transaction, userId: me.Id)
                        .ToAsyncEnumerable()
                        .AnyAsync(transaction.CancellationToken)
                )
                {
                    throw new InvalidOperationException("Target user must be set if not an admin.");
                }

                authorUser = Internal_EnsureExists(
                    await Resources
                        .GetUsers(transaction, id: request.AuthorUserId)
                        .ToAsyncEnumerable()
                        .FirstOrDefaultAsync(transaction.CancellationToken)
                );
            }

            FileAccess[] fileAccesses = await Resources
                .GetFileAccesses(
                    transaction,
                    targetUser,
                    targetGroup,
                    targetFile,
                    authorUser,
                    request.Level
                )
                .ApplyPagination(request.Pagination)
                .ToAsyncEnumerable()
                .WhereAwait(
                    async (fileAccess) =>
                    {
                        File file = await Resources
                            .GetFiles(transaction, id: fileAccess.FileId)
                            .ToAsyncEnumerable()
                            .FirstAsync(transaction.CancellationToken);

                        FileAccessResult? fileAccessResult = await Resources.FindFileAccess(
                            transaction,
                            file,
                            me,
                            userAuthentication,
                            FileAccessLevel.Read
                        );

                        return fileAccessResult != null;
                    }
                )
                .ToArrayAsync(transaction.CancellationToken);

            return new()
            {
                FileAccesses = fileAccesses
                    .Select((fileAccess) => JToken.FromObject(fileAccess).ToString())
                    .ToArray()
            };
        };

    private sealed record class GetFileStarsRequest
    {
        [BsonElement("fileId")]
        public required ObjectId? FileId;

        [BsonElement("userId")]
        public required ObjectId? UserId;

        [BsonElement("pagination")]
        public required PaginationOptions? Pagination;
    };

    private sealed record class GetFileStarsResponse
    {
        [BsonElement("fileStars")]
        public required string[] FileStars;
    };

    private RequestHandler<GetFileStarsRequest, GetFileStarsResponse> GetFileStars =>
        async (transaction, request) =>
        {
            UnlockedUserAuthentication userAuthentication = Internal_EnsureAuthentication();
            User me = await Internal_Me(transaction, userAuthentication);

            User? user = null;
            File? file = null;

            if (request.UserId != null)
            {
                if (
                    request.UserId != me.Id
                    && !await Resources
                        .GetAdminAccesses(transaction, userId: me.Id)
                        .ToAsyncEnumerable()
                        .AnyAsync(transaction.CancellationToken)
                )
                {
                    throw new InvalidOperationException(
                        "Insufficient permissions to get other user's starred files."
                    );
                }

                user = Internal_EnsureExists(
                    await Resources
                        .GetUsers(transaction, id: request.UserId)
                        .ToAsyncEnumerable()
                        .FirstOrDefaultAsync(transaction.CancellationToken)
                );
            }

            if (request.FileId != null)
            {
                file = await Internal_GetFile(transaction, me, request.FileId);
                FileAccessResult fileAccessResult =
                    await Resources.FindFileAccess(
                        transaction,
                        file,
                        me,
                        userAuthentication,
                        FileAccessLevel.Read
                    ) ?? throw new InvalidOperationException("Insufficient permissions.");
            }

            FileStar[] fileStars = await Resources
                .GetFileStars(transaction, file, user)
                .ApplyPagination(request.Pagination)
                .ToAsyncEnumerable()
                .WhereAwait(
                    async (fileStar) =>
                    {
                        File file = await Resources
                            .GetFiles(transaction, id: fileStar.FileId)
                            .ToAsyncEnumerable()
                            .FirstAsync(transaction.CancellationToken);

                        FileAccessResult? fileAccessResult = await Resources.FindFileAccess(
                            transaction,
                            file,
                            me,
                            userAuthentication,
                            FileAccessLevel.Read
                        );

                        return fileAccessResult != null;
                    }
                )
                .ToArrayAsync(transaction.CancellationToken);

            return new()
            {
                FileStars = fileStars
                    .Select((fileStar) => JToken.FromObject(fileStar).ToString())
                    .ToArray()
            };
        };

    private sealed record class GetFilePathRequest
    {
        [BsonElement("fileId")]
        public required ObjectId FileId;

        [BsonElement("pagination")]
        public required PaginationOptions? Pagination;
    }

    private sealed record class GetFilePathResponse
    {
        [BsonElement("path")]
        public required string[] Path;
    }

    private RequestHandler<GetFilePathRequest, GetFilePathResponse> GetFilePath =>
        async (transaction, request) =>
        {
            UnlockedUserAuthentication userAuthentication = Internal_EnsureAuthentication();
            User me = await Internal_Me(transaction, userAuthentication);

            File currentFile = await Internal_GetFile(transaction, me, request.FileId);
            FileAccessResult result =
                await Resources.FindFileAccess(
                    transaction,
                    currentFile,
                    me,
                    userAuthentication,
                    FileAccessLevel.Read
                )
                ?? throw new InvalidOperationException(
                    "Insufficient permissions to access the file."
                );

            File rootFile =
                result.FileAccess != null
                    ? result.File
                    : await Resources.GetRootFolder(transaction, me);

            List<File> path = [currentFile];
            while (true)
            {
                currentFile = await Internal_GetFile(transaction, me, currentFile.ParentId);

                if (path.Last().Id != currentFile.Id)
                {
                    path.Add(currentFile);
                }

                if (currentFile.Id == rootFile.Id)
                {
                    break;
                }
            }

            return new()
            {
                Path = path.Reverse<File>()
                    .Select((entry) => JToken.FromObject(entry).ToString())
                    .ToArray(),
            };
        };

    private sealed partial class UploadFileRequest
    {
        [BsonElement("parentFileId")]
        public required ObjectId ParentFileId;

        [BsonElement("name")]
        public required string Name;
    }

    private sealed partial class UploadFileResponse
    {
        [BsonElement("streamId")]
        public required ulong StreamId;
    };

    private RequestHandler<UploadFileRequest, UploadFileResponse> UploadFile =>
        async (transaction, request) =>
        {
            ConnectionContext context = GetContext();

            UnlockedUserAuthentication userAuthentication = Internal_EnsureAuthentication();
            User me = await Internal_Me(transaction, userAuthentication);

            File parentFolder = await Internal_GetFile(transaction, me, request.ParentFileId);
            FileAccessResult fileAccessResult =
                await Resources.FindFileAccess(
                    transaction,
                    parentFolder,
                    me,
                    userAuthentication,
                    FileAccessLevel.ReadWrite
                ) ?? throw new InvalidOperationException("Insufficient permissions.");

            UnlockedFile file = await Resources.CreateFile(
                transaction,
                me,
                fileAccessResult.File,
                FileType.File,
                request.Name
            );

            FileContent fileContent = await Resources.GetMainFileContent(transaction, file);
            FileSnapshot fileSnapshot =
                await Resources.GetLatestFileSnapshot(transaction, file, fileContent)
                ?? await Resources.CreateFileSnapshot(
                    transaction,
                    file,
                    fileContent,
                    userAuthentication,
                    null
                );

            ulong fileStreamId = context.NextFileStreamId++;
            System.IO.Stream stream = await Resources.CreateWriteStream(
                transaction,
                file,
                fileContent,
                fileSnapshot,
                userAuthentication,
                false
            );

            if (!context.FileStreams.TryAdd(fileStreamId, stream))
            {
                throw new InvalidOperationException("Failed ");
            }

            return new() { StreamId = fileStreamId, };
        };

    private sealed record class UploadBufferRequest
    {
        [BsonElement("streamId")]
        public required ulong StreamId;

        [BsonElement("data")]
        public required byte[] Data;
    };

    private sealed record class UploadBufferResponse { };

    private RequestHandler<UploadBufferRequest, UploadBufferResponse> UploadBuffer =>
        async (transaction, request) =>
        {
            ConnectionContext context = GetContext();

            if (!context.FileStreams.TryGetValue(request.StreamId, out var stream))
            {
                throw new InvalidOperationException("Invalid stream ID.");
            }

            await stream.WriteAsync(request.Data, transaction.CancellationToken);
            return new() { };
        };

    private record class FinishBufferRequest
    {
        [BsonElement("streamId")]
        public required ulong StreamId;
    }

    private record class FinishBufferResponse { }

    private RequestHandler<FinishBufferRequest, FinishBufferResponse> FinishBuffer =>
        (transaction, request) =>
        {
            ConnectionContext context = GetContext();

            if (!context.FileStreams.TryRemove(request.StreamId, out _))
            {
                throw new InvalidOperationException("Invalid stream ID.");
            }

            return Task.FromResult<FinishBufferResponse>(new() { });
        };

    private sealed class CreateFolderRequest
    {
        [BsonElement("parentFileId")]
        public required ObjectId ParentFileId;

        [BsonElement("name")]
        public required string Name;
    }

    private sealed class CreateFolderResponse
    {
        [BsonElement("file")]
        public required string File;
    }

    private RequestHandler<CreateFolderRequest, CreateFolderResponse> CreateFolder =>
        async (transaction, request) =>
        {
            ConnectionContext context = GetContext();

            UnlockedUserAuthentication userAuthentication = Internal_EnsureAuthentication();
            User me = await Internal_Me(transaction, userAuthentication);

            File parentFolder = await Internal_GetFile(transaction, me, request.ParentFileId);
            FileAccessResult fileAccessResult =
                await Resources.FindFileAccess(
                    transaction,
                    parentFolder,
                    me,
                    userAuthentication,
                    FileAccessLevel.ReadWrite
                ) ?? throw new InvalidOperationException("Insufficient permissions.");

            UnlockedFile file = await Resources.CreateFile(
                transaction,
                me,
                fileAccessResult.File,
                FileType.Folder,
                request.Name
            );

            return new() { File = JToken.FromObject(file.Original).ToString(), };
        };

    private sealed class GetFileMimeRequest
    {
        [BsonElement("fileId")]
        public required ObjectId FileId;
    }

    private sealed class GetFileMimeResponse
    {
        [BsonElement("fileMimeType")]
        public required string FileMimeType;
    }

    private RequestHandler<GetFileMimeRequest, GetFileMimeResponse> GetFileMime =>
        async (transaction, request) =>
        {
            ConnectionContext context = GetContext();

            UnlockedUserAuthentication userAuthentication = Internal_EnsureAuthentication();
            User me = await Internal_Me(transaction, userAuthentication);

            File file = await Internal_GetFile(transaction, me, request.FileId);
            FileAccessResult fileAccessResult =
                await Resources.FindFileAccess(
                    transaction,
                    file,
                    me,
                    userAuthentication,
                    FileAccessLevel.ReadWrite
                ) ?? throw new InvalidOperationException("Insufficient permissions.");

            FileContent fileContent = await Resources.GetMainFileContent(transaction, file);
            FileSnapshot fileSnapshot =
                await Resources.GetLatestFileSnapshot(transaction, file, fileContent)
                ?? await Resources.CreateFileSnapshot(
                    transaction,
                    fileAccessResult.File,
                    fileContent,
                    userAuthentication,
                    null
                );

            System.IO.Stream stream = await Resources.CreateReadStream(
                transaction,
                fileAccessResult.File,
                fileContent,
                fileSnapshot
            );

            MimeDetective.Storage.Definition? definition = await Server.MimeDetector.Inspect(
                stream,
                transaction.CancellationToken
            );

            return new() { FileMimeType = definition?.File.MimeType ?? "application/octet-stream" };
        };

    private sealed class GetFileContentsRequest
    {
        [BsonElement("fileId")]
        public required ObjectId FileId;

        [BsonElement("pagination")]
        public required PaginationOptions? Pagination;
    }

    private sealed class GetFileContentsResponse
    {
        [BsonElement("fileContents")]
        public required string[] FileContents;
    }

    private RequestHandler<GetFileContentsRequest, GetFileContentsResponse> GetFileContents =>
        async (transaction, request) =>
        {
            ConnectionContext context = GetContext();

            UnlockedUserAuthentication userAuthentication = Internal_EnsureAuthentication();
            User me = await Internal_Me(transaction, userAuthentication);

            File file = await Internal_GetFile(transaction, me, request.FileId);
            FileAccessResult fileAccessResult =
                await Resources.FindFileAccess(
                    transaction,
                    file,
                    me,
                    userAuthentication,
                    FileAccessLevel.ReadWrite
                ) ?? throw new InvalidOperationException("Insufficient permissions.");

            FileContent[] fileContents = await Resources
                .GetFileContents(transaction, file)
                .ApplyPagination(request.Pagination)
                .ToAsyncEnumerable()
                .ToArrayAsync(transaction.CancellationToken);

            return new()
            {
                FileContents = fileContents
                    .Select((fileContent) => JToken.FromObject(fileContent).ToString())
                    .ToArray()
            };
        };

    private sealed class GetFileSnapshotsRequest
    {
        [BsonElement("fileId")]
        public required ObjectId FileId;

        [BsonElement("fileContentId")]
        public required ObjectId FileContentId;

        [BsonElement("pagination")]
        public required PaginationOptions? Pagination;
    }

    private sealed class GetFileSnapshotsResponse
    {
        [BsonElement("fileSnapshots")]
        public required string[] FileSnapshots;
    }

    private RequestHandler<GetFileSnapshotsRequest, GetFileSnapshotsResponse> GetFileSnapshots =>
        async (transaction, request) =>
        {
            ConnectionContext context = GetContext();

            UnlockedUserAuthentication userAuthentication = Internal_EnsureAuthentication();
            User me = await Internal_Me(transaction, userAuthentication);

            File file = await Internal_GetFile(transaction, me, request.FileId);
            FileAccessResult fileAccessResult =
                await Resources.FindFileAccess(
                    transaction,
                    file,
                    me,
                    userAuthentication,
                    FileAccessLevel.ReadWrite
                ) ?? throw new InvalidOperationException("Insufficient permissions.");

            FileContent fileContent =
                await Resources
                    .GetFileContents(transaction, file, id: request.FileContentId)
                    .ToAsyncEnumerable()
                    .FirstOrDefaultAsync(transaction.CancellationToken)
                ?? throw new InvalidOperationException("Invalid file content id.");

            FileSnapshot[] fileSnapshots = await Resources
                .GetFileSnapshots(transaction, file, fileContent)
                .ApplyPagination(request.Pagination)
                .ToAsyncEnumerable()
                .ToArrayAsync(transaction.CancellationToken);

            return new()
            {
                FileSnapshots =
                [
                    .. fileSnapshots.Select(
                        (fileSnapshot) => JToken.FromObject(fileSnapshot).ToString()
                    )
                ]
            };
        };

    private sealed class ReadFileRequest
    {
        [BsonElement("fileId")]
        public required ObjectId FileId;

        [BsonElement("fileContentId")]
        public required ObjectId? FileContentId;

        [BsonElement("fileSnapshotId")]
        public required ObjectId? FileSnapshotId;

        [BsonElement("position")]
        public required long Position;

        [BsonElement("length")]
        public required long Length;
    }

    private sealed class ReadFileResponse
    {
        [BsonElement("fileData")]
        public required byte[] FileData;
    }

    private RequestHandler<ReadFileRequest, ReadFileResponse> ReadFile =>
        async (transaction, request) =>
        {
            ConnectionContext context = GetContext();

            UnlockedUserAuthentication userAuthentication = Internal_EnsureAuthentication();
            User me = await Internal_Me(transaction, userAuthentication);

            File file = await Internal_GetFile(transaction, me, request.FileId);
            FileAccessResult fileAccessResult =
                await Resources.FindFileAccess(
                    transaction,
                    file,
                    me,
                    userAuthentication,
                    FileAccessLevel.ReadWrite
                ) ?? throw new InvalidOperationException("Insufficient permissions.");

            FileContent fileContent =
                await Resources
                    .GetFileContents(transaction, file, id: request.FileContentId)
                    .ToAsyncEnumerable()
                    .FirstOrDefaultAsync(transaction.CancellationToken)
                ?? (
                    request.FileContentId != null
                        ? throw new InvalidOperationException("Invalid file content id.")
                        : await Resources.GetMainFileContent(transaction, file)
                );

            FileSnapshot fileSnapshot =
                await Resources
                    .GetFileSnapshots(transaction, file, fileContent, request.FileSnapshotId)
                    .ToAsyncEnumerable()
                    .FirstOrDefaultAsync(transaction.CancellationToken)
                ?? (
                    request.FileSnapshotId != null
                        ? throw new InvalidOperationException()
                        : await Resources.GetLatestFileSnapshot(transaction, file, fileContent)
                            ?? throw new InvalidOperationException("No file content found.")
                );

            return new()
            {
                FileData =
                [
                    .. await Resources.ReadFile(
                        transaction,
                        fileAccessResult.File,
                        fileContent,
                        fileSnapshot,
                        request.Position,
                        request.Length
                    )
                ]
            };
        };

    private sealed class UpdateFileRequest
    {
        [BsonElement("fileId")]
        public required ObjectId FileId;

        [BsonElement("fileSnapshotId")]
        public required ObjectId? FileSnapshotId;

        [BsonElement("position")]
        public required long Position;
    }

    private sealed class UpdateFileResponse
    {
        [BsonElement("streamId")]
        public required ulong StreamId;
    }

    private RequestHandler<UpdateFileRequest, UpdateFileResponse> UpdateFile =>
        async (transaction, request) =>
        {
            ConnectionContext context = GetContext();

            UnlockedUserAuthentication userAuthentication = Internal_EnsureAuthentication();
            User me = await Internal_Me(transaction, userAuthentication);

            File file = await Internal_GetFile(transaction, me, request.FileId);
            FileAccessResult fileAccessResult =
                await Resources.FindFileAccess(
                    transaction,
                    file,
                    me,
                    userAuthentication,
                    FileAccessLevel.ReadWrite
                ) ?? throw new InvalidOperationException("Insufficient permissions.");

            FileContent fileContent = await Resources.GetMainFileContent(transaction, file);

            FileSnapshot fileSnapshot =
                await Resources
                    .GetFileSnapshots(transaction, file, fileContent, request.FileSnapshotId)
                    .ToAsyncEnumerable()
                    .FirstOrDefaultAsync(transaction.CancellationToken)
                ?? (
                    request.FileSnapshotId != null
                        ? throw new InvalidOperationException()
                        : await Resources.GetLatestFileSnapshot(transaction, file, fileContent)
                            ?? throw new InvalidOperationException("No file content found.")
                );

            ulong streamId = context.NextFileStreamId++;
            System.IO.Stream stream = await Resources.CreateWriteStream(
                transaction,
                fileAccessResult.File,
                fileContent,
                fileSnapshot,
                userAuthentication,
                createNewSnapshot: false,
                currentTransaction: true
            );

            return new() { StreamId = streamId };
        };

    private sealed record class AmIAdminRequest { }

    private sealed record class AmIAdminResponse
    {
        [BsonElement("amIAdmin")]
        public required bool AmIAdmin;
    }

    private RequestHandler<AmIAdminRequest, AmIAdminResponse> AmIAdmin =>
        async (transaction, request) =>
        {
            ConnectionContext context = GetContext();

            UnlockedUserAuthentication userAuthentication = Internal_EnsureAuthentication();
            User me = await Internal_Me(transaction, userAuthentication);

            return new()
            {
                AmIAdmin = await Resources
                    .GetAdminAccesses(transaction, me.Id)
                    .ToAsyncEnumerable()
                    .AnyAsync(transaction.CancellationToken)
            };
        };

    private sealed record class GetFileLogsRequest
    {
        [BsonElement("fileId")]
        public required ObjectId FileId;

        [BsonElement("fileContentId")]
        public required ObjectId? FileContentId;

        [BsonElement("fileSnapshotId")]
        public required ObjectId? FileSnapshotId;

        [BsonElement("userId")]
        public required ObjectId? UserId;
    }

    private sealed record class GetFileLogsResponse
    {
        [BsonElement("fileLogs")]
        public required string[] FileLogs;
    }

    private RequestHandler<GetFileLogsRequest, GetFileLogsResponse> GetFileLogs =>
        async (transaction, request) =>
        {
            UnlockedUserAuthentication userAuthentication = Internal_EnsureAuthentication();
            User me = await Internal_Me(transaction, userAuthentication);

            if (
                me.Id != request.UserId
                && !await Resources
                    .GetAdminAccesses(transaction, me.Id)
                    .ToAsyncEnumerable()
                    .AnyAsync(transaction)
            )
            {
                throw new InvalidOperationException(
                    "User ID other than self is not allowed when not an administrator."
                );
            }

            User? user =
                request.UserId != null
                    ? await Resources
                        .GetUsers(transaction, id: request.UserId)
                        .ToAsyncEnumerable()
                        .FirstOrDefaultAsync(transaction)
                    : null;

            File? file = request.FileId!= null
                ? await Internal_GetFile(transaction, me, request.FileId)
                : null;
            FileAccessResult fileAccessResult =
                await Resources.FindFileAccess(
                    transaction,
                    file,
                    me,
                    userAuthentication,
                    FileAccessLevel.ReadWrite
                ) ?? throw new InvalidOperationException("Insufficient permissions.");

            // return new() { };
        };
}
