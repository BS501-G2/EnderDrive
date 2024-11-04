using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderDrive.Server.Services;

using System.Collections.Generic;
using Commons.Memory;
using Resources;
using Utilities;

public sealed partial class Connection
{
    private static void RegisterHandler<S, R>(
        ConnectionContext context,
        ServerSideRequestCode code,
        RequestHandler<S, R> handler
    )
    {
        if (
            !context.Handlers.TryAdd(
                code,
                async (transaction, requestBuffer) =>
                {
                    try
                    {
                        S request = requestBuffer.DeserializeData<S>();
                        R response = await handler(transaction, request);

                        return ConnectionPacket<ResponseCode>.Create(ResponseCode.OK, response);
                    }
                    catch (ConnectionResponseException exception)
                    {
                        return ConnectionPacket<ResponseCode>.Create(
                            exception.Code,
                            exception.Data
                        );
                    }
                }
            )
        )
        {
            throw new InvalidOperationException("Handler is already added.");
        }
    }

    private void RegisterHandlers(ConnectionContext context)
    {
        void registerHandler<S, R>(ServerSideRequestCode code, RequestHandler<S, R> handler) =>
            RegisterHandler<S, R>(context, code, handler);

        registerHandler(ServerSideRequestCode.SetupRequirements, SetupRequirements);
        registerHandler(ServerSideRequestCode.CreateAdmin, CreateAdmin);
        registerHandler(ServerSideRequestCode.ResolveUsername, ResolveUsername);
        registerHandler(ServerSideRequestCode.AuthenticatePassword, AuthenticatePassword);
        registerHandler(ServerSideRequestCode.AuthenticateGoogle, AuthenticateGoogle);
        registerHandler(ServerSideRequestCode.AuthenticateToken, AuthenticateToken);
        registerHandler(ServerSideRequestCode.WhoAmI, WhoAmI);
        registerHandler(ServerSideRequestCode.Deauthenticate, Deauthenticate);
        registerHandler(ServerSideRequestCode.GetUser, GetUser);
        registerHandler(ServerSideRequestCode.GetUsers, GetUsers);
        registerHandler(ServerSideRequestCode.GetFile, GetFile);
        registerHandler(ServerSideRequestCode.GetFiles, ScanFolder);
        registerHandler(ServerSideRequestCode.GetFileAccesses, GetFileAccesses);
        registerHandler(ServerSideRequestCode.GetFileStars, GetFileStars);
        registerHandler(ServerSideRequestCode.GetFilePath, GetFilePath);
        registerHandler(ServerSideRequestCode.UploadFile, UploadFile);
    }

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
                : await Resources.GetUserRootFile(transaction, me)
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
                    .GetUsers(transaction, minRole: UserRole.Admin, maxRole: UserRole.Admin)
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
                    .GetUsers(transaction, minRole: UserRole.Admin, maxRole: UserRole.Admin)
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
                await Resources.FindFileAccess(transaction, file, me)
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

    private RequestHandler<GetFilesRequest, GetFilesResponse> ScanFolder =>
        async (transaction, request) =>
        {
            UnlockedUserAuthentication userAuthentication = Internal_EnsureAuthentication();
            User me = await Internal_Me(transaction, userAuthentication);

            File? parentFolder = await Internal_GetFile(transaction, me, request.ParentFolderId);
            if (parentFolder != null)
            {
                FileAccessResult result =
                    await Resources.FindFileAccess(transaction, parentFolder, me)
                    ?? throw new InvalidOperationException("No access to this file");
            }

            User? ownerUser =
                request.OwnerUserId != null
                    ? await Resources
                        .GetUsers(transaction, id: request.OwnerUserId)
                        .ToAsyncEnumerable()
                        .FirstOrDefaultAsync(transaction.CancellationToken)
                    : null;

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

        [BsonElement("targetGroupId")]
        public required ObjectId? TargetGroupId;

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
            }

            if (request.TargetUserId != null)
            {
                if (
                    targetFile == null
                    && request.TargetUserId != me.Id
                    && me.Role.HasFlag(UserRole.Admin)
                )
                {
                    throw new InvalidOperationException(
                        "Target file must be set if the target user id is not yourself."
                    );
                }

                FileAccessResult? result =
                    targetFile != null
                        ? await Resources.FindFileAccess(
                            transaction,
                            targetFile,
                            me,
                            FileAccessLevel.Manage
                        ) ?? throw new InvalidOperationException("User has no access to file.")
                        : null;

                targetUser = Internal_EnsureExists(
                    await Resources
                        .GetUsers(transaction, id: request.TargetUserId)
                        .ToAsyncEnumerable()
                        .FirstOrDefaultAsync(transaction.CancellationToken)
                );
            }
            else if (request.TargetGroupId != null)
            {
                if (targetFile == null)
                {
                    throw new InvalidOperationException("Target file must be set.");
                }

                FileAccessResult? result =
                    targetFile != null
                        ? await Resources.FindFileAccess(
                            transaction,
                            targetFile,
                            me,
                            FileAccessLevel.Manage
                        ) ?? throw new InvalidOperationException("User has no access to file.")
                        : null;

                targetGroup = Internal_EnsureExists(
                    await Resources
                        .GetGroups(transaction, id: request.TargetGroupId)
                        .ToAsyncEnumerable()
                        .FirstOrDefaultAsync(transaction.CancellationToken)
                );
            }

            FileAccess[] fileAccesses = await Resources
                .GetFileAccesses(
                    transaction,
                    targetUser: targetUser,
                    targetGroup,
                    targetFile,
                    authorUser,
                    request.Level
                )
                .ApplyPagination(request.Pagination)
                .ToAsyncEnumerable()
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
                if (request.UserId != me.Id && me.Role < UserRole.Admin)
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
                    await Resources.FindFileAccess(transaction, file, me, FileAccessLevel.Read)
                    ?? throw new InvalidOperationException("Insufficient permissions.");
            }

            FileStar[] fileStars = await Resources
                .GetFileStars(transaction, file, user)
                .ApplyPagination(request.Pagination)
                .ToAsyncEnumerable()
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
                await Resources.FindFileAccess(transaction, currentFile, me, FileAccessLevel.Read)
                ?? throw new InvalidOperationException(
                    "Insufficient permissions to access the file."
                );

            File rootFile =
                result.FileAccess != null
                    ? result.File
                    : await Resources.GetUserRootFile(transaction, me);

            List<File> path = [currentFile];
            do
            {
                currentFile = await Internal_GetFile(transaction, me, currentFile.ParentId);
                path.Insert(0, currentFile);
            } while (currentFile.Id != rootFile.Id);

            return new()
            {
                Path = path.Select((entry) => JToken.FromObject(entry).ToString()).ToArray(),
            };
        };

    private sealed partial class UploadFileRequest
    {
        [BsonElement("parentFileId")]
        public required ObjectId ParentFileId;

        [BsonElement("name")]
        public required string Name;

        [BsonElement("content")]
        public required byte[] Content;
    }

    private sealed partial class UploadFileResponse { };

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
                    FileAccessLevel.ReadWrite
                ) ?? throw new InvalidOperationException("Insufficient permissions.");

            UnlockedFile file = await Resources.CreateFile(
                transaction,
                me,
                parentFolder,
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

            await Resources.WriteFile(
                transaction,
                file,
                fileContent,
                fileSnapshot,
                userAuthentication,
                0,
                request.Content
            );

            return new() { };
        };
}

public enum ServerSideRequestCode : byte
{
    Echo,

    WhoAmI,

    AuthenticatePassword,
    AuthenticateGoogle,
    AuthenticateToken,
    Deauthenticate,

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

    UploadFile
}
