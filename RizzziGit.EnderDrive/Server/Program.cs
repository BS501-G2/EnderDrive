using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RizzziGit.EnderDrive.Server;

using Commons.Logging;
using Commons.Memory;
using Core;
using Resources;

public static partial class Program
{
  public static Task Main(string[] args) =>
    Task.Run(async () =>
    {
      Server server = new(Environment.CurrentDirectory);

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
