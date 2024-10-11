using System;

namespace RizzziGit.EnderDrive.Utilities;

public static class ExceptionExtensions
{
    public static string ToPrintable(this Exception exception) =>
        $"{exception.Message}{(exception.StackTrace != null ? $"\n{exception.StackTrace}" : "")}";
}
