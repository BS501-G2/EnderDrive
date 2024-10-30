using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;

namespace RizzziGit.EnderDrive.Server.Services;

using Commons.Memory;
using Commons.Services;
using Core;
using Resources;

public sealed class SessionManagerContext
{
    public required List<Session> Sessions;
}

public sealed class Session
{
    public required ulong Id;
    public required UnlockedUserAuthentication? User;
}

public sealed class SessionManager(ApiServer apiServer)
    : Service<SessionManagerContext>("Sessions", apiServer)
{
    protected override Task<SessionManagerContext> OnStart(
        CancellationToken startupCancellationToken,
        CancellationToken serviceCancellationToken
    )
    {
        List<Session> sessions = [];

        return Task.FromResult<SessionManagerContext>(new() { Sessions = sessions });
    }
}
