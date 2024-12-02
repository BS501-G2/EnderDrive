using System;
using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server;

using Core;

public static partial class Program
{
  public static Task Main(string[] _) =>
    Task.Run(async () =>
    {
      EnderDriveServer server = new();

      ConsoleCancelEventHandler? handler = null;
      Console.CancelKeyPress += handler = (origin, args) =>
      {
        Console.WriteLine();
        server.Stop().Wait();

        Console.CancelKeyPress -= handler;
      };

      server.Logged += (level, scope, message, time) =>
        Console.WriteLine($"[{time}] [{level}] [{string.Join('/', scope)}] {message}");

      await server.Start();
      await server.Watch();
    });
}
