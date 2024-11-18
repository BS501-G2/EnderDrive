using System.Threading.Tasks;

namespace RizzziGit.EnderDrive;

public static class Program
{
  private delegate Task MainMethod(string[] args);

  public static Task Main(string[] args)
  {
    MainMethod main = args[0] switch
    {
      "server" => Server.Program.Main,
      "client" => Client.Program.Main,

      _ => throw new System.NotImplementedException(),
    };

    return main(args[1..]);
  }
}
