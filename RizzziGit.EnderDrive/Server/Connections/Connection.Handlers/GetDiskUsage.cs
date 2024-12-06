using System;
using System.IO;

namespace RizzziGit.EnderDrive.Server.Connections;

public sealed partial class Connection
{
  private sealed record GetDiskUsageRequest { }

  private sealed record GetDiskUsageResponse
  {
    public required long DiskUsage;
    public required long DiskTotal;
  }

  private static RequestHandler<GetDiskUsageRequest, GetDiskUsageResponse> GetDiskUsage =>
    async (request, cancellationToken) =>
    {
      DriveInfo info = new(Directory.GetDirectoryRoot(Environment.CurrentDirectory));

      return new() { DiskUsage = info.TotalSize - info.TotalFreeSpace, DiskTotal = info.TotalSize };
    };
}
