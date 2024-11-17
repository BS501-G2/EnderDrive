using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace RizzziGit.EnderDrive.Server.Connections;

using Commons.Collections;
using Commons.Memory;
using Commons.Utilities;
using Resources;

public sealed partial class Connection
{
	private async void RunStream(
		UnlockedFile file,
		FileContent fileContent,
		FileSnapshot fileSnapshot,
		UnlockedUserAuthentication userAuthentication,
		TaskCompletionSource<ObjectId> source
	) =>
		await Resources.Transact(
			async (
				transaction
			) =>
			{
				ConnectionContext context =
					GetContext();

				ObjectId objectId =
					ObjectId.GenerateNewId();
				WaitQueue<ConnectionByteStream.Feed> queue =
					new(
						0
					);
				ConnectionByteStream stream =
					new(
						objectId,
						queue
					);

				if (
					!context.FileStreams.TryAdd(
						objectId,
						stream
					)
				)
				{
					source.SetException(
						new InvalidOperationException(
							"Failed to create a file stream."
						)
					);

					return;
				}

				source.SetResult(
					objectId
				);

				long offset =
					0;

				async Task<bool> handleFeed(
					ConnectionByteStream.Feed feed,
					CancellationToken cancellationToken
				)
				{
					switch (
						feed
					)
					{
						case ConnectionByteStream.Feed.Write(
							TaskCompletionSource source,
							CompositeBuffer bytes,
							_
						):
						{
							try
							{
								await Resources.WriteFile(
									transaction,
									file,
									fileContent,
									fileSnapshot,
									userAuthentication,
									offset,
									bytes
								);
								offset +=
									bytes.Length;

								source.SetResult();
							}
							catch (Exception exception)
							{
								source.SetException(
									exception
								);
							}

							break;
						}

						case ConnectionByteStream.Feed.Read(
							TaskCompletionSource<CompositeBuffer> source,
							long count,
							_
						):
						{
							try
							{
								CompositeBuffer bytes =
									await Resources.ReadFile(
										transaction,
										file,
										fileContent,
										fileSnapshot,
										offset,
										count
									);

								offset +=
									bytes.Length;

								source.SetResult(
									bytes
								);
							}
							catch (Exception exception)
							{
								source.SetException(
									exception
								);
							}

							break;
						}

						case ConnectionByteStream.Feed.SetPosition(
							TaskCompletionSource source,
							long newOffset,
							_
						):
						{
							try
							{
								long length =
									await Resources.GetFileSize(
										transaction,
										fileSnapshot
									);

								if (
									newOffset
									> length
								)
								{
									source.SetException(
										new ArgumentOutOfRangeException()
									);
								}
								else
								{
									offset =
										newOffset;
									source.SetResult();
								}
							}
							catch (Exception exception)
							{
								source.SetException(
									exception
								);
							}

							break;
						}

						case ConnectionByteStream.Feed.GetPosition(
							TaskCompletionSource<long> source,
							_
						):
						{
							try
							{
								long length =
									await Resources.GetFileSize(
										transaction,
										fileSnapshot
									);

								source.SetResult(
									length
								);
							}
							catch (Exception exception)
							{
								source.SetException(
									exception
								);
							}

							break;
						}

						case ConnectionByteStream.Feed.GetLength(
							TaskCompletionSource<long> source,
							_
						):
						{
							try
							{
								long length =
									await Resources.GetFileSize(
										transaction,
										fileSnapshot
									);

								source.SetResult(
									length
								);
							}
							catch (Exception exception)
							{
								source.SetException(
									exception
								);
							}

							break;
						}

						case ConnectionByteStream.Feed.Close(
							TaskCompletionSource source,
							_
						):
						{
							source.SetResult();
							return false;
						}
					}

					return true;
				}

				try
				{
					await foreach (
						ConnectionByteStream.Feed feed in queue.WithCancellation(
							transaction.CancellationToken
						)
					)
					{
						using CancellationTokenSource linkedCancellationTokenSource =
							feed.CancellationToken.Link(
								transaction.CancellationToken
							);

						if (
							!await handleFeed(
								feed,
								linkedCancellationTokenSource.Token
							)
						)
						{
							break;
						}
					}
				}
				catch (Exception exception)
				{
					await Resources.DeleteFile(
						transaction,
						file
					);

					Error(
						exception
					);
					queue.Dispose(
						exception
					);
				}
				finally
				{
					context.FileStreams.TryRemove(
						objectId,
						out _
					);
				}
			},
			CancellationToken.None
		);
}
