using System;
using System.Threading;
using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server.Services;

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
        await Resources.Transact(
            async (transaction) =>
            {
                try
                {
                    ConnectionPacket<ServerSideRequestCode> request =
                        ConnectionPacket<ServerSideRequestCode>.Deserialize(
                            webConnectionRequest.Data
                        );

                    ConnectionPacket<ResponseCode> response = !context.Handlers.TryGetValue(
                        request.Code,
                        out RawRequestHandler? handler
                    )
                        ? ConnectionPacket<ResponseCode>.Create(
                            ResponseCode.NoHandlerFound,
                            new { }
                        )
                        : await handler(transaction, request);

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
            },
            cancellationToken
        );
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
        registerHandler(ServerSideRequestCode.GetFiles, GetFiles);
        registerHandler(ServerSideRequestCode.GetFileAccesses, GetFileAccesses);
        registerHandler(ServerSideRequestCode.GetFileStars, GetFileStars);
        registerHandler(ServerSideRequestCode.GetFilePath, GetFilePath);
        registerHandler(ServerSideRequestCode.UploadFile, UploadFile);
        registerHandler(ServerSideRequestCode.FinishBuffer, FinishBuffer);
        registerHandler(ServerSideRequestCode.UploadBuffer, UploadBuffer);
        registerHandler(ServerSideRequestCode.CreateFolder, CreateFolder);
        registerHandler(ServerSideRequestCode.GetFileMime, GetFileMime);
        registerHandler(ServerSideRequestCode.GetFileContents, GetFileContents);
        registerHandler(ServerSideRequestCode.GetFileSnapshots, GetFileSnapshots);
        registerHandler(ServerSideRequestCode.AmIAdmin, AmIAdmin);
        registerHandler(ServerSideRequestCode.ReadFile, ReadFile);
        registerHandler(ServerSideRequestCode.UpdateFile, UpdateFile);

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

    UploadFile,
    UploadBuffer,
    FinishBuffer,
    CreateFolder,
    GetFileMime,
    GetFileContents,
    GetFileSnapshots,
    ReadFile,
    UpdateFile,

    AmIAdmin,

GetFileLogs,
    ScanFile
}
