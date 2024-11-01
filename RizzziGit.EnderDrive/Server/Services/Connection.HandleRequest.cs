using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;

namespace RizzziGit.EnderDrive.Server.Services;

using System.Linq;
using Commons.Memory;
using Commons.Net.WebConnection;
using Resources;

public enum ServerSideRequestCode : byte
{
    Echo,

    WhoAmI,
    AuthenticatePassword,
    AuthenticateGoogle,

    CreateAdmin,
    SetupRequirements
}

public sealed partial class Connection
{
    private async Task HandleRequest(
        WebConnectionRequest webConnectionRequest,
        CancellationToken cancellationToken
    )
    {
        await Resources.Transact(
            async (transaction) =>
            {
                ConnectionPacket<ResponseCode> responsePacket;
                try
                {
                    ConnectionPacket<ServerSideRequestCode> requestPacket =
                        ConnectionPacket<ServerSideRequestCode>.Deserialize(
                            webConnectionRequest.Data
                        );

                    responsePacket = await HandleRequest(transaction, requestPacket);
                }
                catch (Exception exception)
                {
                    if (exception is ConnectionResponseException connectionResponseException)
                        responsePacket = ConnectionPacket<ResponseCode>.Create(
                            connectionResponseException.Code,
                            connectionResponseException.Data
                        );
                    else
                    {
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
                                exception
                                    is ConnectionResponseException webConnectionResponseException
                                    ? webConnectionResponseException.Data
                                    : exception.Message
                            );
                        }

                        return;
                    }
                }

                webConnectionRequest.SendResponse(responsePacket.Serialize());
            },
            cancellationToken
        );
    }

    private async Task<ConnectionPacket<ResponseCode>> HandleRequest(
        ResourceTransaction transaction,
        ConnectionPacket<ServerSideRequestCode> request
    )
    {
        switch (request.Code)
        {
            case ServerSideRequestCode.SetupRequirements:
            {
                return ConnectionPacket<ResponseCode>.Create(
                    ResponseCode.OK,
                    await HandleSetupRequirements(
                        transaction,
                        request.DeserializeData<SetupRequirementsRequest>()
                    )
                );
            }

            case ServerSideRequestCode.CreateAdmin:
            {
                return ConnectionPacket<ResponseCode>.Create(
                    ResponseCode.OK,
                    await HandleCreateAdmin(
                        transaction,
                        request.DeserializeData<CreateAdminRequest>()
                    )
                );
            }

            default:
                throw new ConnectionResponseException(ResponseCode.InternalError, []);
        }
    }

    public sealed record SetupRequirementsRequest { };

    public sealed record SetupRequirementsResponse
    {
        public required bool AdminSetupRequired;
    };

    private async Task<SetupRequirementsResponse> HandleSetupRequirements(
        ResourceTransaction transaction,
        SetupRequirementsRequest request
    )
    {
        return new()
        {
            AdminSetupRequired = !await Resources
                .GetUsers(transaction, minRole: UserRole.Admin, maxRole: UserRole.Admin)
                .AnyAsync(transaction.CancellationToken)
        };
    }

    public sealed record CreateAdminRequest()
    {
        public required string Username;
        public required string Password;
        public required string ConfirmPassword;

        public required string LastName;
        public required string FirstName;
        public required string? MiddleName;
        public required string? DisplayName;
    };

    public sealed record CreateAdminResponse() { };

    private async Task<CreateAdminResponse> HandleCreateAdmin(
        ResourceTransaction transaction,
        CreateAdminRequest request
    )
    {
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
    }
}
