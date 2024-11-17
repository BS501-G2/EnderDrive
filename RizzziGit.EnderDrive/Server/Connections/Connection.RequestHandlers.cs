namespace RizzziGit.EnderDrive.Server.Connections;

using Resources;

public sealed partial class Connection
{
	private void RegisterHandlers(
		ConnectionContext context
	)
	{
		void registerHandler<
			S,
			R
		>(
			ServerSideRequestCode code,
			RequestHandler<
				S,
				R
			> handler
		) =>
			RegisterHandler<
				S,
				R
			>(
				context,
				code,
				handler
			);

		void registerTransactedHandler<
			S,
			R
		>(
			ServerSideRequestCode code,
			TransactedRequestHandler<
				S,
				R
			> handler
		) =>
			RegisterTransactedHandler<
				S,
				R
			>(
				context,
				code,
				handler
			);

		void registerAuthenticatedHandler<
			S,
			R
		>(
			ServerSideRequestCode code,
			AuthenticatedRequestHandler<
				S,
				R
			> handler,
			UserRole[]? requiredIncludeRole =
				null,
			UserRole[]? requiredExcludeRole =
				null
		) =>
			RegisterAuthenticatedHandler(
				context,
				code,
				handler,
				requiredIncludeRole,
				requiredExcludeRole
			);

		void registerFileHandler<
			S,
			R
		>(
			ServerSideRequestCode code,
			FileRequestHandler<
				S,
				R
			> handler,
			FileType? fileType =
				null,
			FileAccessLevel? fileAccessLevel =
				null
		)
			where S : BaseFileRequest =>
			RegisterFileHandler(
				context,
				code,
				handler,
				fileAccessLevel,
				fileType
			);

		registerHandler(
			ServerSideRequestCode.AmILoggedIn,
			AmILoggedIn
		);
		registerTransactedHandler(
			ServerSideRequestCode.SetupRequirements,
			SetupRequirements
		);
		registerTransactedHandler(
			ServerSideRequestCode.CreateAdmin,
			CreateAdmin
		);
		registerTransactedHandler(
			ServerSideRequestCode.ResolveUsername,
			ResolveUsername
		);
		registerTransactedHandler(
			ServerSideRequestCode.AuthenticatePassword,
			AuthenticatePassword
		);
		registerTransactedHandler(
			ServerSideRequestCode.AuthenticateGoogle,
			AuthenticateGoogle
		);
		registerTransactedHandler(
			ServerSideRequestCode.AuthenticateToken,
			AuthenticateToken
		);
		registerTransactedHandler(
			ServerSideRequestCode.AmIAdmin,
			AmIAdmin
		);
		registerAuthenticatedHandler(
			ServerSideRequestCode.Me,
			Me
		);
		registerAuthenticatedHandler(
			ServerSideRequestCode.Deauthenticate,
			Deauthenticate
		);
		registerAuthenticatedHandler(
			ServerSideRequestCode.GetUser,
			GetUser
		);
		registerAuthenticatedHandler(
			ServerSideRequestCode.GetUsers,
			GetUsers
		);
		registerAuthenticatedHandler(
			ServerSideRequestCode.GetFiles,
			GetFiles
		);
		registerAuthenticatedHandler(
			ServerSideRequestCode.GetFileAccesses,
			GetFileAccesses
		);
		registerAuthenticatedHandler(
			ServerSideRequestCode.GetFileStars,
			GetFileStars
		);
		registerAuthenticatedHandler(
			ServerSideRequestCode.GetFileLogs,
			GetFileLogs
		);
		registerAuthenticatedHandler(
			ServerSideRequestCode.CreateFile,
			CreateFile
		);
		registerAuthenticatedHandler(
			ServerSideRequestCode.OpenStream,
			OpenStream
		);
		registerAuthenticatedHandler(
			ServerSideRequestCode.CreateNews,
			CreateNews,
			[
				UserRole.NewsEditor,
			]
		);
		registerAuthenticatedHandler(
			ServerSideRequestCode.DeleteNews,
			DeleteNews,
			[
				UserRole.NewsEditor,
			]
		);
		registerAuthenticatedHandler(
			ServerSideRequestCode.GetNews,
			GetNews
		);
		registerAuthenticatedHandler(
			ServerSideRequestCode.SetFileStar,
			SetFileStar
		);
		registerAuthenticatedHandler(
			ServerSideRequestCode.GetFileStar,
			GetFileStar
		);
		registerFileHandler(
			ServerSideRequestCode.GetFilePath,
			GetFilePath,
			null,
			FileAccessLevel.Read
		);
		registerFileHandler(
			ServerSideRequestCode.GetFile,
			GetFile,
			null,
			FileAccessLevel.Read
		);
		registerFileHandler(
			ServerSideRequestCode.CreateFolder,
			CreateFolder,
			null,
			FileAccessLevel.ReadWrite
		);
		registerFileHandler(
			ServerSideRequestCode.GetFileMime,
			GetFileMime,
			null,
			FileAccessLevel.Read
		);
		registerFileHandler(
			ServerSideRequestCode.GetFileSize,
			GetFileSize,
			null,
			FileAccessLevel.Read
		);
		registerFileHandler(
			ServerSideRequestCode.GetFileContents,
			GetFileContents,
			null,
			FileAccessLevel.Read
		);
		registerFileHandler(
			ServerSideRequestCode.GetFileSnapshots,
			GetFileSnapshots,
			null,
			FileAccessLevel.Read
		);
		registerFileHandler(
			ServerSideRequestCode.ScanFile,
			ScanFile,
			null,
			FileAccessLevel.Read
		);
		registerFileHandler(
			ServerSideRequestCode.GetMainFileContent,
			GetMainFileContent
		);
		registerFileHandler(
			ServerSideRequestCode.GetLatestFileSnapshot,
			GetLatestFileSnapshot
		);
		registerHandler(
			ServerSideRequestCode.CloseStream,
			CloseStream
		);
		registerHandler(
			ServerSideRequestCode.ReadStream,
			ReadStream
		);
		registerHandler(
			ServerSideRequestCode.WriteStream,
			WriteStream
		);
		registerHandler(
			ServerSideRequestCode.SetPosition,
			SetPosition
		);
		registerHandler(
			ServerSideRequestCode.GetStreamSize,
			GetStreamSize
		);
		registerHandler(
			ServerSideRequestCode.GetStreamPosition,
			GetStreamPosition
		);
	}
}

public enum ServerSideRequestCode
	: byte
{
	Echo,

	Me,
	AmILoggedIn,
	AmIAdmin,

	AuthenticatePassword,
	AuthenticateGoogle,
	AuthenticateToken,
	Deauthenticate,
	GetAgreement,

	CreateAdmin,
	SetupRequirements,

	ResolveUsername,

	GetUser,
	GetUsers,

	GetFile,
	GetFiles,
	GetFileAccesses,
	GetFileStars,
	GetFilePath,
	GetFileMime,
	GetFileSize,
	GetFileContents,
	GetMainFileContent,
	GetFileSnapshots,
	GetLatestFileSnapshot,
	GetFileLogs,
	ScanFile,
	CreateFolder,
	CreateFile,
	SetFileStar,
	GetFileStar,

	OpenStream,
	CloseStream,
	ReadStream,
	WriteStream,
	SetPosition,
	GetStreamSize,
	GetStreamPosition,

	CreateNews,
	DeleteNews,
	GetNews,
}
