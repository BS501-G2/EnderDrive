using System;
using System.Threading;
using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server.Services;

using Commons.Memory;
using MessagePack;

public enum ClientSideRequestCode : byte
{
    Ping,
    Notify
}

public sealed partial class Connection
{
    public async Task<R> SendRequest<S, R>(
        ClientSideRequestCode code,
        S data,
        CancellationToken cancellationToken
    )
    {
        var context = GetContext();

        ConnectionPacket<ClientSideRequestCode> request =
            ConnectionPacket<ClientSideRequestCode>.Create(code, data);

        ConnectionPacket<ResponseCode> response = ConnectionPacket<ResponseCode>.Deserialize(
            (
                await context.Internal.SendRequest(request.Serialize(), cancellationToken)
            ).ToByteArray()
        );

        if (response.Code != ResponseCode.OK)
        {
            throw new ConnectionResponseException(response.Code, response.Data);
        }

        return response.DeserializeData<R>();
    }
}
