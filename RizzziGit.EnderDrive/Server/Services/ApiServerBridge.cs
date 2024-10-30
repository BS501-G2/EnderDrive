// using System;
// using System.Diagnostics;
// using System.IO;
// using System.Threading;
// using System.Threading.Tasks;

// namespace RizzziGit.EnderDrive.Server.Services;

// using System.Runtime.ExceptionServices;
// using Commons.Services;
// using RizzziGit.EnderDrive.Utilities;

// public sealed class ApiServerBridgeContext
// {
//     public required Process Process;
// }

// public sealed partial class SocketIoBridge(ApiServer server)
//     : Service<ApiServerBridgeContext>("Socket.IO Bridge", server)
// {
//     protected override Task<ApiServerBridgeContext> OnStart(
//         CancellationToken startupCancellationToken,
//         CancellationToken serviceCancellationToken
//     )
//     {
//         string workingPath = Path.GetFullPath("../RizzziGit.EnderDrive.Proxy");

//         ProcessStartInfo startInfo =
//             new()
//             {
//                 FileName = "/usr/bin/env",
//                 UseShellExecute = false,
//                 WorkingDirectory = workingPath,
//                 RedirectStandardInput = true,
//                 RedirectStandardError = true,
//                 RedirectStandardOutput = true
//             };

//         startInfo.ArgumentList.Add("/usr/bin/node");
//         startInfo.ArgumentList.Add(workingPath);

//         Process? process =
//             Process.Start(startInfo)
//             ?? throw new InvalidOperationException("Failed to start node process.");

//         return Task.FromResult<ApiServerBridgeContext>(new() { Process = process });
//     }

//     protected override async Task OnRun(
//         ApiServerBridgeContext context,
//         CancellationToken cancellationToken
//     )
//     {
//         async Task main(StreamReader stream, string name)
//         {
//             while (true)
//             {
//                 cancellationToken.ThrowIfCancellationRequested();

//                 string? line = await stream.ReadLineAsync(cancellationToken);

//                 if (line == null)
//                 {
//                     break;
//                 }

//                 if (string.IsNullOrWhiteSpace(line))
//                 {
//                     continue;
//                 }

//                 Debug(line, name);
//             }
//         }

//         await await Task.WhenAny(
//             main(context.Process.StandardError, "stderr"),
//             main(context.Process.StandardOutput, "stdout"),
//             context.Process.WaitForExitAsync(cancellationToken)
//         );
//     }

//     protected override async Task OnStop(ApiServerBridgeContext context, ExceptionDispatchInfo? exception)
//     {
//         context.Process.Kill();
//         if (context.Process.HasExited == false)
//         {
//             await context.Process.WaitForExitAsync();
//         }

//         if (context.Process.ExitCode != 0 && context.Process.ExitCode != 130)
//         {
//             throw new InvalidOperationException(
//                 $"Node process exited with code {context.Process.ExitCode}."
//             );
//         }
//         else
//         {
//             Info($"Node process exited with code {context.Process.ExitCode}.", "Non-Fatal Error");
//         }
//     }
// }
