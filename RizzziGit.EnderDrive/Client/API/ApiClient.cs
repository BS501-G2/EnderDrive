// using System;
// using System.Net.WebSockets;
// using System.Threading;
// using System.Threading.Tasks;

// namespace RizzziGit.EnderDrive.Client.API;

// using Commons.Net.WebConnection;
// using Commons.Services;

// public sealed class ApiClientParams { }

// public sealed class ApiClient(IService downstream)
//     : Service<ApiClientParams>("API Client", downstream)
// {
//     protected override async Task<ApiClientParams> OnStart(
//         CancellationToken startupCancellationToken,
//         CancellationToken serviceCancellationToken
//     )
//     {
//         ClientWebSocket client = new();
//         await client.ConnectAsync(new Uri("ws://localhost:8080"), startupCancellationToken);

//         // HybridWebSocket hybridWebSocket = new(client, false, "WebSocket Port", this);

//         // await StartServices([hybridWebSocket], startupCancellationToken);

//         // return new() { HybridWebSocket = hybridWebSocket };

//         return new();
//     }

//     protected override async Task OnRun(
//         ApiClientParams context,
//         CancellationToken cancellationToken
//     )
//     {
//         await Task.Delay(-1, cancellationToken);
//     }

//     protected override async Task OnStop(ApiClientParams context, ExceptionDispatchInfo? exception)
//     {
//         // await StopServices(context.HybridWebSocket);
//     }
// }
