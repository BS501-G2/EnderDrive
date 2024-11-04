/* eslint-disable @typescript-eslint/no-explicit-any */
import { get, type Writable } from 'svelte/store';
import { ResponseCode, type ClientContext } from './client';

export interface AuthenticationToken {
	userId: string;
	token: string;
}

export function getServerSideFunctions(
	getRequestFunc: () => ClientContext['request'],
	onStartup: (handler: () => Promise<void>) => void,
	storedAuthenticationToken: Writable<AuthenticationToken | null>
) {
	async function request(code: ServerSideRequestCode, data: any): Promise<any> {
		const response = await getRequestFunc()({ Code: code, Data: data });

		if (response.Code !== ResponseCode.OK) {
			throw new Error('Failed');
		}

		return response.Data;
	}

	const functions = {
		getSetupRequirements: (): Promise<{ adminSetupRequired: boolean }> =>
			request(ServerSideRequestCode.SetupRequirements, {}),

		createAdmin: (
			username: string,
			password: string,
			confirmPassword: string,
			firstName: string,
			middleName: string | null,
			lastName: string,
			displayName: string | null
		): Promise<object> =>
			request(ServerSideRequestCode.CreateAdmin, {
				username,
				password,
				confirmPassword,
				lastName,
				firstName,
				middleName,
				displayName
			}),

		resolveUsername: async (username: string): Promise<string | null> => {
			const result = await request(ServerSideRequestCode.ResolveUsername, { username });
			return result.userId;
		},

		authenticatePassword: async (
			userId: string,
			password: string
		): Promise<{ token: string; userId: string }> => {
			const { token } = await request(ServerSideRequestCode.AuthenticatePassword, {
				userId,
				password
			});

			storedAuthenticationToken.set({ userId, token });

			return { token, userId };
		},

		authenticateGoogle: async (googleToken: string) => {
			const { token, userId } = await request(ServerSideRequestCode.AuthenticateGoogle, {
				googleToken
			});

			storedAuthenticationToken.set({ userId, token });

			return { token, userId };
		},

		authenticateToken: async (userId: string, token: string): Promise<void> => {
			const { renewedToken } = await request(ServerSideRequestCode.AuthenticateToken, {
				userId,
				token
			});

			if (renewedToken != null) {
				storedAuthenticationToken.set({ userId, token: renewedToken });
			}
		},

		deauthenticate: async () => {
			const result = await request(ServerSideRequestCode.Deauthenticate, {});

			storedAuthenticationToken.set(null);

			return result;
		},

		whoAmI: (): Promise<{ userId?: string }> => request(ServerSideRequestCode.WhoAmI, {}),

		getUser: async (userId: string): Promise<UserResource | null> => {
			const result = await request(ServerSideRequestCode.GetUser, { userId });

			if (result.user != null) {
				return JSON.parse(result.user);
			}

			return null;
		},

		getUsers: async (
			searchString?: string,
			minRole?: UserRole,
			maxRole?: UserRole,
			username?: string,
			id?: string,
			offset?: number,
			count?: number
		) => {
			const { users } = await request(ServerSideRequestCode.GetUsers, {
				searchString,
				minRole,
				maxRole,
				username,
				id,
				pagination: { offset, count }
			});

			return users.map((user: any) => JSON.parse(user)) as UserResource[];
		},

		getFile: async (fileId?: string) => {
			const { file } = await request(ServerSideRequestCode.GetFile, { fileId });

			return JSON.parse(file);
		},

		getFiles: async (
			parentFolderId?: string,
			fileType?: FileType,
			name?: string,
			ownerUserId?: string,
			trashOptions?: TrashOptions,
			offset?: number,
			count?: string
		) => {
			const { files } = await request(ServerSideRequestCode.GetFiles, {
				parentFolderId,
				fileType,
				name,
				ownerUserId,
				trashOptions,
				pagination: { offset, count }
			});

			return files.map((file: any) => JSON.parse(file)) as FileResource[];
		},

		getFileAccesses: async (
			targetUserId?: string,
			targetGroupId?: string,
			targetFileId?: string,
			authorUserId?: string,
			level?: FileAccessLevel,
			id?: string,
			offset?: number,
			count?: number
		) => {
			const { fileAccesses } = await request(ServerSideRequestCode.GetFileAccesses, {
				targetUserId,
				targetGroupId,
				targetFileId,
				authorUserId,
				level,
				id,
				pagination: { offset, count }
			});

			return fileAccesses.map((fileAccess: any) => JSON.parse(fileAccess)) as FileAccessResource[];
		},

		getFileStars: async (fileId?: string, userId?: string, offset?: number, count?: number) => {
			const { fileStars } = await request(ServerSideRequestCode.GetFileStars, {
				fileId,
				userId,
				pagination: { offset, count }
			});

			return fileStars.map((fileStar: any) => JSON.parse(fileStar)) as FileStarResource[];
		},

		getFilePath: async (fileId?: string, offset?: number, count?: string) => {
			const { path } = await request(ServerSideRequestCode.GetFilePath, {
				fileId,
				pagination: { offset, count }
			});

			return path.map((entry: any) => JSON.parse(entry)) as FileResource[];
		}
	};

	onStartup(async () => {
		const token = get(storedAuthenticationToken);
		if (token == null || token.userId == null || token.token == null) {
			return;
		}

		try {
			await functions.authenticateToken(token.userId, token.token);
		} catch (error: any) {
			if (error.message.includes('Invalid user id.')) {
				storedAuthenticationToken.set(null);
			}
		}
	});

	return functions;
}

export enum ServerSideRequestCode {
	Echo,

	WhoAmI,

	AuthenticatePassword,
	AuthenticateGoogle,
	AuthenticateToken,
	Deauthenticate,

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

	UploadFile
}

export interface SearchParams {
	count?: number;
	offset?: number;
}

export enum UserRole {
	None = 0,
	Admin = 1 << 0,
	NewsEditor = 1 << 1
}

export interface UserResource {
	username: string;
	firstName: string;
	middleName?: string;
	lastName: string;
	displayName?: string;
	role: UserRole;
}

export interface FileResource {
	parentId?: string;
	ownerUserId?: string;
	name: string;
	type: FileType;
}

export interface FileAccessResource {
	fileId: string;
	authorUserId: string;
	targetEntity?: FileTargetEntity;
	level: FileAccessLevel;
}

export interface FileTargetEntity {
	entityType: FileAccessTargetEntityType;
	entityId: string;
}

export enum FileAccessTargetEntityType {
	User,
	Group
}

export enum FileAccessLevel {
	None,
	Read,
	ReadWrite,
	Manage,
	Full
}

export enum FileType {
	File,
	Folder
}

export enum TrashOptions {
	NotIncluded,
	Included,
	Exclusive
}
export interface FileStarResource {
	fileId: string;
	userId: string;

	creatTime: Date;
}
