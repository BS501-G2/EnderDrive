using System;
using System.Threading;
using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server.Connections;

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

    private void RegisterTransactedHandler<S, R>(
        ConnectionContext context,
        ServerSideRequestCode code,
        TransactedRequestHandler<S, R> handler
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
                        R response = await Resources.Transact(
                            (transaction) => handler(transaction, request),
                            cancellationToken
                        );

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

    private void RegisterHandlers(ConnectionContext context)
    {
        void registerTransactedHandler<S, R>(
            ServerSideRequestCode code,
            TransactedRequestHandler<S, R> handler
        ) => RegisterTransactedHandler<S, R>(context, code, handler);

        void registerHandler<S, R>(ServerSideRequestCode code, RequestHandler<S, R> handler) =>
            RegisterHandler<S, R>(context, code, handler);

        registerTransactedHandler(ServerSideRequestCode.SetupRequirements, SetupRequirements);
        registerTransactedHandler(ServerSideRequestCode.CreateAdmin, CreateAdmin);
        registerTransactedHandler(ServerSideRequestCode.ResolveUsername, ResolveUsername);
        registerTransactedHandler(ServerSideRequestCode.AuthenticatePassword, AuthenticatePassword);
        registerTransactedHandler(ServerSideRequestCode.AuthenticateGoogle, AuthenticateGoogle);
        registerTransactedHandler(ServerSideRequestCode.AuthenticateToken, AuthenticateToken);
        registerTransactedHandler(ServerSideRequestCode.WhoAmI, WhoAmI);
        registerTransactedHandler(ServerSideRequestCode.Deauthenticate, Deauthenticate);
        registerTransactedHandler(ServerSideRequestCode.GetUser, GetUser);
        registerTransactedHandler(ServerSideRequestCode.GetUsers, GetUsers);
        registerTransactedHandler(ServerSideRequestCode.GetFile, GetFile);
        registerTransactedHandler(ServerSideRequestCode.GetFiles, GetFiles);
        registerTransactedHandler(ServerSideRequestCode.GetFileAccesses, GetFileAccesses);
        registerTransactedHandler(ServerSideRequestCode.GetFileStars, GetFileStars);
        registerTransactedHandler(ServerSideRequestCode.GetFilePath, GetFilePath);
        registerTransactedHandler(ServerSideRequestCode.CreateFolder, CreateFolder);
        registerTransactedHandler(ServerSideRequestCode.GetFileMime, GetFileMime);
        registerTransactedHandler(ServerSideRequestCode.GetFileContents, GetFileContents);
        registerTransactedHandler(ServerSideRequestCode.GetFileSnapshots, GetFileSnapshots);
        registerTransactedHandler(ServerSideRequestCode.AmIAdmin, AmIAdmin);
        registerTransactedHandler(ServerSideRequestCode.GetFileLogs, GetFileLogs);
        registerTransactedHandler(ServerSideRequestCode.ScanFile, ScanFile);
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

    CreateFolder,
    GetFileMime,
    GetFileContents,
    GetFileSnapshots,

    AmIAdmin,

    GetFileLogs,
    ScanFile,

    OpenStream,
    CloseStream,
    ReadStream,
    WriteStream,
    SeekStream,
    GetStreamSize,
    GetStreamPosition,
}
