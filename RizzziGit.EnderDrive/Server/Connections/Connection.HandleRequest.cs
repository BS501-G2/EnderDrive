using System;
using System.Threading;
using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server.Connections;

using System.Linq;
using Commons.Net.WebConnection;
using Resources;

public sealed partial class Connection
{
    private async Task HandleRequest(
        ConnectionContext context,
        WebConnectionRequest webConnectionRequest,
        CancellationToken cancellationToken
    )
    {
        try
        {
            ConnectionPacket<ServerSideRequestCode> request =
                ConnectionPacket<ServerSideRequestCode>.Deserialize(webConnectionRequest.Data);

            ConnectionPacket<ResponseCode> response = !context.Handlers.TryGetValue(
                request.Code,
                out RawRequestHandler? handler
            )
                ? ConnectionPacket<ResponseCode>.Create(ResponseCode.NoHandlerFound, new { })
                : await handler(request, cancellationToken);

            webConnectionRequest.SendResponse(response.Serialize());
        }
        catch (Exception exception)
        {
            Error(exception);

            if (
                exception is OperationCanceledException operationCanceledException
                && operationCanceledException.CancellationToken
                    == webConnectionRequest.CancellationToken
            )
            {
                webConnectionRequest.SendCancelResponse();
            }
            else
            {
                webConnectionRequest.SendErrorResponse(
                    exception is ConnectionResponseException webConnectionResponseException
                        ? webConnectionResponseException.Data
                        : exception.Message
                );
            }

            return;
        }
    }

    private static void RegisterHandler<S, R>(
        ConnectionContext context,
        ServerSideRequestCode code,
        RequestHandler<S, R> handler
    )
    {
        if (
            !context.Handlers.TryAdd(
                code,
                async (requestBuffer, cancellationToken) =>
                {
                    try
                    {
                        S request = requestBuffer.DeserializeData<S>();
                        R response = await handler(request, cancellationToken);

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

    private void RegisterTransactedHandler<S, R>(
        ConnectionContext context,
        ServerSideRequestCode code,
        TransactedRequestHandler<S, R> handler
    ) =>
        RegisterHandler<S, R>(
            context,
            code,
            (request, cancellationToken) =>
                Resources.Transact(
                    (transaction) => handler(transaction, request),
                    cancellationToken
                )
        );

    private void RegisterAuthenticatedHandler<S, R>(
        ConnectionContext context,
        ServerSideRequestCode code,
        AuthenticatedRequestHandler<S, R> handler,
        UserRole[]? requiredIncludeRole = null,
        UserRole[]? requiredExcludeRole = null
    ) =>
        RegisterTransactedHandler<S, R>(
            context,
            code,
            async (transaction, request) =>
            {
                UnlockedUserAuthentication userAuthentication = Internal_EnsureAuthentication();
                User me = await Internal_Me(transaction, userAuthentication);

                if (
                    (requiredIncludeRole != null || requiredExcludeRole != null)
                    && !await Resources
                        .GetAdminAccesses(transaction, userId: me.Id)
                        .ToAsyncEnumerable()
                        .AnyAsync(transaction.CancellationToken)
                )
                {
                    if (
                        requiredIncludeRole != null
                        && !me.Roles.Intersect(requiredIncludeRole).Any()
                    )
                    {
                        throw new InvalidOperationException(
                            $"Requires role: {string.Join(", ", requiredIncludeRole)}"
                        );
                    }

                    if (
                        requiredExcludeRole != null
                        && me.Roles.Intersect(requiredExcludeRole).Any()
                    )
                    {
                        throw new InvalidOperationException(
                            $"User has role: {string.Join(", ", requiredExcludeRole)}"
                        );
                    }
                }

                return await handler(transaction, request, userAuthentication, me);
            }
        );

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

        registerTransactedHandler(ServerSideRequestCode.SetupRequirements, SetupRequirements);
        registerTransactedHandler(ServerSideRequestCode.CreateAdmin, CreateAdmin);
        registerTransactedHandler(ServerSideRequestCode.ResolveUsername, ResolveUsername);
        registerTransactedHandler(ServerSideRequestCode.AuthenticatePassword, AuthenticatePassword);
        registerTransactedHandler(ServerSideRequestCode.AuthenticateGoogle, AuthenticateGoogle);
        registerTransactedHandler(ServerSideRequestCode.AuthenticateToken, AuthenticateToken);
        registerHandler(ServerSideRequestCode.WhoAmI, WhoAmI);
        registerAuthenticatedHandler(ServerSideRequestCode.Deauthenticate, Deauthenticate);
        registerAuthenticatedHandler(ServerSideRequestCode.GetUser, GetUser);
        registerAuthenticatedHandler(ServerSideRequestCode.GetUsers, GetUsers);
        registerAuthenticatedHandler(ServerSideRequestCode.GetFile, GetFile);
        registerTransactedHandler(ServerSideRequestCode.GetFiles, GetFiles);
        registerTransactedHandler(ServerSideRequestCode.GetFileAccesses, GetFileAccesses);
        registerTransactedHandler(ServerSideRequestCode.GetFileStars, GetFileStars);
        registerTransactedHandler(ServerSideRequestCode.GetFilePath, GetFilePath);
        registerTransactedHandler(ServerSideRequestCode.CreateFolder, CreateFolder);
        registerTransactedHandler(ServerSideRequestCode.GetFileMime, GetFileMime);
        registerAuthenticatedHandler(ServerSideRequestCode.GetFileSize, GetFileSize);
        registerTransactedHandler(ServerSideRequestCode.GetFileContents, GetFileContents);
        registerTransactedHandler(ServerSideRequestCode.GetFileSnapshots, GetFileSnapshots);
        registerTransactedHandler(ServerSideRequestCode.AmIAdmin, AmIAdmin);
        registerTransactedHandler(ServerSideRequestCode.GetFileLogs, GetFileLogs);
        registerTransactedHandler(ServerSideRequestCode.ScanFile, ScanFile);
        registerAuthenticatedHandler(ServerSideRequestCode.CreateFile, CreateFile);
        registerAuthenticatedHandler(ServerSideRequestCode.OpenStream, OpenStream);
        registerHandler(ServerSideRequestCode.CloseStream, CloseStream);
        registerHandler(ServerSideRequestCode.ReadStream, ReadStream);
        registerHandler(ServerSideRequestCode.WriteStream, WriteStream);
        registerHandler(ServerSideRequestCode.SetPosition, SetPosition);
        registerHandler(ServerSideRequestCode.GetStreamSize, GetStreamSize);
        registerHandler(ServerSideRequestCode.GetPosition, GetPosition);
        registerAuthenticatedHandler(ServerSideRequestCode.GetMainFileContent, GetMainFileContent);
        registerAuthenticatedHandler(
            ServerSideRequestCode.GetLatestFileSnapshot,
            GetLatestFileSnapshot
        );
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
        registerAuthenticatedHandler(ServerSideRequestCode.SetFileStar, SetFileStar);
        registerAuthenticatedHandler(ServerSideRequestCode.GetFileStar, GetFileStar);
    }
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

    GetFileMime,
    GetFileSize,
    GetFileContents,
    GetMainFileContent,
    GetFileSnapshots,
    GetLatestFileSnapshot,

    AmIAdmin,

    GetFileLogs,
    ScanFile,

    CreateFolder,
    CreateFile,

    OpenStream,
    CloseStream,
    ReadStream,
    WriteStream,
    SetPosition,
    GetStreamSize,
    GetPosition,

    CreateNews,
    DeleteNews,
    GetNews,
    SetFileStar,
    GetFileStar,
}
