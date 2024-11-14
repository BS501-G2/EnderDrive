// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Net.Sockets;
// using System.Runtime.CompilerServices;
// using System.Threading;
// using System.Threading.Tasks;
// using RizzziGit.Commons.Memory;

// namespace RizzziGit.EnderDrive.Utilities;

// public delegate IAsyncEnumerable<byte[]> AsyncQuickStreamSendLoop(
//     CancellationToken cancellationToken
// );

// public delegate ValueTask AsyncQuickStreamReceiveLoop(
//     IAsyncEnumerable<byte[]> bytes,
//     CancellationToken cancellationToken
// );
// public delegate ValueTask<long> AsyncQuickStreamSeek(long offset, SeekOrigin origin);
// public delegate ValueTask<long> AsyncQuickStreamLength();

// public delegate IEnumerable<byte[]> QuickStreamSendLoop(CancellationToken cancellationToken);
// public delegate IEnumerable<byte[]> QuickStreamReceiveLoop(
//     IEnumerable<byte[]> bytes,
//     CancellationToken cancellationToken
// );
// public delegate long QuickStreamSeek(long offset, SeekOrigin origin);
// public delegate long QuickStreamGetLength();

// internal abstract record StreamLoopParameters
// {
//     private StreamLoopParameters() { }

//     public sealed record Async(
//         AsyncQuickStreamReceiveLoop? ReceiveLoop,
//         AsyncQuickStreamSendLoop? SendLoop,
//         AsyncQuickStreamLength? GetLength,
//         AsyncQuickStreamSeek? Seek
//     ) : StreamLoopParameters;

//     public sealed record Sync(
//         QuickStreamReceiveLoop? ReceiveLoop,
//         QuickStreamSendLoop? SendLoop,
//         QuickStreamGetLength? GetLength,
//         QuickStreamSeek? Seek
//     ) : StreamLoopParameters;
// }

// public class QuickStream : Stream
// {
//     public static QuickStream CreateAsync(
//         AsyncQuickStreamReceiveLoop? receiveLoop,
//         AsyncQuickStreamSendLoop? sendLoop,
//         AsyncQuickStreamLength? getLength,
//         AsyncQuickStreamSeek? seek
//     )
//     {
//         return new(new StreamLoopParameters.Async(receiveLoop, sendLoop, getLength, seek));
//     }

//     public static QuickStream CreateSync(
//         QuickStreamReceiveLoop? receiveLoop,
//         QuickStreamSendLoop? sendLoop,
//         QuickStreamGetLength? getLength,
//         QuickStreamSeek? seek
//     )
//     {
//         return new(new StreamLoopParameters.Sync(receiveLoop, sendLoop, getLength, seek));
//     }

//     private QuickStream(StreamLoopParameters parameters)
//         : base()
//     {
//         CancellationTokenSource = new();
//         Parameters = parameters;
//         Buffer = [];
//     }

//     private readonly CancellationTokenSource CancellationTokenSource;
//     private readonly StreamLoopParameters Parameters;
//     private readonly CompositeBuffer Buffer;

//     public override bool CanRead =>
//         Parameters switch
//         {
//             StreamLoopParameters.Async { ReceiveLoop: not null } => true,
//             StreamLoopParameters.Sync { ReceiveLoop: not null } => true,
//             _ => false,
//         };

//     public override bool CanWrite =>
//         Parameters switch
//         {
//             StreamLoopParameters.Async { SendLoop: not null } => true,
//             StreamLoopParameters.Sync { SendLoop: not null } => true,
//             _ => false,
//         };

//     public override bool CanSeek =>
//         Parameters switch
//         {
//             StreamLoopParameters.Async { Seek: not null } => true,
//             StreamLoopParameters.Sync { Seek: not null } => true,
//             _ => false,
//         };

//     public override long Length =>
//         Parameters switch
//         {
//             StreamLoopParameters.Async { GetLength: AsyncQuickStreamLength getLength } =>
//                 getLength().AsTask().GetAwaiter().GetResult(),

//             StreamLoopParameters.Sync { GetLength: QuickStreamGetLength getLength } => getLength(),

//             _ => throw new NotSupportedException(),
//         };

//     public override long Position
//     {
//         get =>
//             Parameters switch
//             {
//                 StreamLoopParameters.Async { Seek: AsyncQuickStreamSeek seek } => seek(
//                         0,
//                         SeekOrigin.Begin
//                     )
//                     .AsTask()
//                     .GetAwaiter()
//                     .GetResult(),

//                 StreamLoopParameters.Sync { Seek: QuickStreamSeek seek } => seek(
//                     0,
//                     SeekOrigin.Begin
//                 ),

//                 _ => throw new NotSupportedException(),
//             };
//         set
//         {
//             switch (Parameters)
//             {
//                 case StreamLoopParameters.Async { Seek: AsyncQuickStreamSeek seek }:
//                     seek(value, SeekOrigin.Begin).AsTask().GetAwaiter().GetResult();
//                     break;

//                 case StreamLoopParameters.Sync { Seek: QuickStreamSeek seek }:
//                     seek(value, SeekOrigin.Begin);
//                     break;

//                 default:
//                     throw new NotSupportedException();
//             }
//         }
//     }

//     public override void Flush() { }

//     public override int Read(byte[] buffer, int offset, int count)
//     {

//     }

//     public override long Seek(long offset, SeekOrigin origin)
//     {
//         throw new NotImplementedException();
//     }

//     public override void SetLength(long value)
//     {
//         throw new NotImplementedException();
//     }

//     public override void Write(byte[] buffer, int offset, int count)
//     {
//         throw new NotImplementedException();
//     }
// }
