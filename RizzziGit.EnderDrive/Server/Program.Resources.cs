// using System;
// using System.IO;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;

// namespace RizzziGit.EnderDrive.Server;

// using System.Text;
// using Commons.Memory;
// using Core;
// using Resources;

// public static partial class Program
// {
//     private static async Task UserTest(Server server)
//     {
//         ResourceManager resources = server.ResourceManager;

//         var (user, userAuthentication) = await resources.Transact(
//             async (transaction) =>
//             {
//                 (User user, UnlockedUserAuthentication userAuthentication) =
//                     await resources.CreateUser(transaction, "as", "ti", null, "A", null, "test");

//                 UnlockedFile rootFile = (await resources.GetUserRootFile(transaction, user)).Unlock(
//                     userAuthentication
//                 );

//                 UnlockedFile file = await resources.CreateFile(
//                     transaction,
//                     user,
//                     rootFile,
//                     FileType.File,
//                     ""
//                 );

//                 FileContent fileContent = await resources.GetMainFileContent(transaction, file);

//                 FileSnapshot fileSnapshot = await resources.CreateFileSnapshot(
//                     transaction,
//                     file,
//                     fileContent,
//                     userAuthentication,
//                     null
//                 );

//                 byte[] eicar = Encoding.UTF8.GetBytes(
//                     "X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*"
//                 );

//                 await resources.WriteFile(
//                     transaction,
//                     file,
//                     fileContent,
//                     fileSnapshot,
//                     userAuthentication,
//                     0,
//                     eicar
//                 );

//                 UnlockedFileAccess fileAccess = await resources.CreateFileAccess(
//                     transaction,
//                     file,
//                     FileAccessLevel.Read
//                 );

//                 Console.WriteLine(
//                     await resources.ReadFile(
//                         transaction,
//                         fileAccess.UnlockFile(file),
//                         fileContent,
//                         fileSnapshot,
//                         0,
//                         eicar.Length
//                     )
//                 );

//                 return (user, userAuthentication);
//             },
//             CancellationToken.None
//         );

//         await resources.Transact(
//             async (transaction) =>
//             {
//                 await resources.DeleteUser(transaction, user, userAuthentication);
//             }
//         );
//     }
// }
