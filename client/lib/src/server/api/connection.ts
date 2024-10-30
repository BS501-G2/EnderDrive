import { Socket } from "socket.io";
import {
  ApiError,
  ApiErrorType,
  baseConnectionFunctions,
  FileAccessLevel,
  fileIoSize,
  FileLogType,
  FileType,
  ScanFolderSortType,
  SocketWrapper,
  UserResolvePayload,
  UserRole,
  wrapSocket,
} from "../../shared.js";
import {
  UnlockedUserAuthentication,
  UserAuthenticationManager,
} from "../db/user-authentication.js";
import { UserManager, UserResource } from "../db/user.js";
import { UserSessionManager, UserSessionType } from "../db/user-session.js";
import { ServerConnectionManager } from "./connection-manager.js";
import { FileManager, FileResource, UnlockedFileResource } from "../db/file.js";
import { FileContentManager, FileContentResource } from "../db/file-content.js";
import {
  FileSnapshotManager,
  FileSnapshotResource,
} from "../db/file-snapshot.js";
import { FileDataManager } from "../db/file-data.js";
import { FileLogManager, FileLogResource } from "../db/file-log.js";
import { FileAccessManager, FileAccessResource } from "../db/file-access.js";
import { FileStarManager } from "../db/file-star.js";
import { News, ServerFunctions } from "./connection-functions.js";
import { ClientFunctions } from "../../client/core/connection-functions.js";

export interface ServerConnectionContext {
  updateTime: number;
}

export const newsArray: News[] = [];

export class ServerConnection {
  public constructor(
    manager: ServerConnectionManager,
    id: number,
    socket: Socket,
    {
      onDisconnect,
      getContext,
    }: {
      onDisconnect: () => void;
      getContext: () => ServerConnectionContext;
    }
  ) {
    this.#manager = manager;
    this.#id = id;
    this.#wrapper = wrapSocket(
      (this.#io = socket),
      new Proxy(this.#server, {
        get: (target, prop) => {
          return async (...args: Array<any>) => {
            try {
              return await target[prop as keyof ServerFunctions](...args);
            } catch (error) {
              this.#manager.log(
                "error",
                error instanceof Error
                  ? error.stack ?? error.message
                  : `${error}`
              );

              throw error;
            }
          };
        },
      }),
      (func) => this.#onMessage(func)
      // (...message) => {
      //   console.log(...message);
      // }
    );
    this.#getContext = getContext;

    this.#userAuthentication = null;

    this.#io.on("disconnect", onDisconnect);
  }

  readonly #manager: ServerConnectionManager;
  readonly #id: number;
  readonly #io: Socket;
  readonly #wrapper: SocketWrapper<ClientFunctions, ServerFunctions, Socket>;
  readonly #getContext: () => ServerConnectionContext;

  #userAuthentication: UnlockedUserAuthentication | null;

  get context() {
    return this.#getContext();
  }

  get id() {
    return this.#id;
  }

  get currentUserId() {
    return this.#userAuthentication?.userId;
  }

  get #client() {
    return this.#wrapper.funcs;
  }

  #onMessage(func: () => Promise<void>): Promise<void> {
    return this.#manager.server.database.transact(func);
  }

  get #server(): ServerFunctions {
    const server = this.#manager.server;
    const database = this.#manager.server.database;

    const bufferLimit: number = 1024 * 1024 * 512;
    const buffers: Uint8Array[] = [];

    const feedUploadBuffer = (buffer: Uint8Array) => {
      if (getUploadSize() + buffer.length > bufferLimit) {
        ApiError.throw(
          ApiErrorType.InvalidRequest,
          "Upload buffer size limit reached"
        );
      }

      buffers.push(buffer);
      return getUploadSize();
    };

    const getUploadSize = (): number =>
      buffers.reduce((size, buffer) => size + buffer.length, 0);

    const [
      userManager,
      userAuthenticationManager,
      userSessionManager,
      fileAccessManager,
      fileManager,
      fileContentManager,
      fileSnapshotManager,
      fileDataManager,
      fileLogManager,
      fileStarManager,
    ] = database.getManagers(
      UserManager,
      UserAuthenticationManager,
      UserSessionManager,
      FileAccessManager,
      FileManager,
      FileContentManager,
      FileSnapshotManager,
      FileDataManager,
      FileLogManager,
      FileStarManager
    );

    const getFile = async (
      id: number | null,
      authentication: UnlockedUserAuthentication,
      accessLevel: FileAccessLevel = FileAccessLevel.None,
      requireType?: FileType
    ) => {
      const file: FileResource | null =
        id != null
          ? await fileManager.getById(id)
          : await fileManager.getRoot(authentication);

      if (file == null) {
        ApiError.throw(ApiErrorType.NotFound, "File not found");
      }

      if (requireType != null && requireType != file.type) {
        ApiError.throw(ApiErrorType.InvalidRequest, "File type invalid");
      }

      try {
        const unlockedFile = await fileManager.unlock(
          file,
          authentication,
          accessLevel
        );

        return unlockedFile;
      } catch (error: any) {
        console.log(error);
        ApiError.throw(ApiErrorType.Forbidden, `Failed to unlock file #${id}`, {
          cause: error,
        });
      }
    };

    const getSnapshot = async (
      file: UnlockedFileResource,
      fileContent: FileContentResource,
      id: number
    ) => {
      const fileSnapshot = await fileSnapshotManager.first({
        where: [
          ["fileId", "=", file.id],
          ["fileContentId", "=", fileContent.id],
          ["id", "=", id],
        ],
      });

      if (fileSnapshot == null) {
        ApiError.throw(ApiErrorType.InvalidRequest, "File snapshot not found");
      }

      return fileSnapshot;
    };

    const resolveUser = async (
      resolve: UserResolvePayload
    ): Promise<UserResource> => {
      let user: UserResource | null = null;

      if (resolve[0] === "userId") {
        user = await userManager.getById(resolve[1]);
      } else if (resolve[0] === "username") {
        user = await userManager.getByUsername(resolve[1]);
      }

      if (user == null) {
        ApiError.throw(ApiErrorType.NotFound, "User not found");
      }

      return user;
    };

    const requireRole = async (
      authentication: UnlockedUserAuthentication,
      role: UserRole
    ) => {
      const user = await userManager.getById(authentication.userId);

      if (user == null) {
        ApiError.throw(ApiErrorType.Unauthorized, "Invalid user");
      }

      if (role > user.role) {
        ApiError.throw(ApiErrorType.Forbidden, "Insufficient role");
      }
    };

    const requireAuthenticated = <T extends boolean>(
      authenticated: T = true as T
    ): T extends true ? UnlockedUserAuthentication : void => {
      if (authenticated) {
        if (this.#userAuthentication == null) {
          ApiError.throw(ApiErrorType.Unauthorized, "Not logged in");
        }

        return this.#userAuthentication as never;
      } else {
        if (this.currentUserId != null) {
          ApiError.throw(ApiErrorType.Conflict, "Already logged in");
        }

        return undefined as never;
      }
    };

    const currentUser = (authentication: UnlockedUserAuthentication) =>
      resolveUser(["userId", authentication.userId]);

    const sortFiles = async (
      files: FileResource[],
      sort: [type: ScanFolderSortType, desc: boolean]
    ): Promise<FileResource[]> => {
      const [sortType, sortDesc] = sort;

      console.log(files);

      if (sortType === ScanFolderSortType.FileName) {
        return files.sort((a, b) =>
          sortDesc ? b.name.localeCompare(a.name) : a.name.localeCompare(b.name)
        );
      } else if (
        sortType === ScanFolderSortType.DateModified ||
        sortType === ScanFolderSortType.ContentSize
      ) {
        const filesWithData = await Promise.all(
          files.map(async (file) => {
            const fileContent = await fileContentManager.getMain(file);
            const latestSnapshot = await fileSnapshotManager.getLatest(
              file,
              fileContent
            );

            return [file, latestSnapshot] as [
              file: FileResource,
              snapshot: FileSnapshotResource
            ];
          })
        );

        return filesWithData
          .toSorted((a, b) => {
            const [, aSnapshot] = a;
            const [, bSnapshot] = b;

            if (sortType === ScanFolderSortType.DateModified) {
              if (sortDesc) {
                return bSnapshot.createTime - aSnapshot.createTime;
              } else {
                return aSnapshot.createTime - bSnapshot.createTime;
              }
            } else if (sortType === ScanFolderSortType.ContentSize) {
              if (sortDesc) {
                return bSnapshot.size - aSnapshot.size;
              } else {
                return aSnapshot.size - bSnapshot.size;
              }
            } else {
              if (sortDesc) {
                return bSnapshot.id - aSnapshot.id;
              } else {
                return aSnapshot.id - bSnapshot.id;
              }
            }
          })
          .map(([file]) => file);
      }

      return files;
    };

    const serverFunctions: ServerFunctions = {
      ...baseConnectionFunctions,

      isAuthenticationValid: async (authentication) => {
        requireAuthenticated(false);

        const { userId, userSessionId, userSessionKey } = authentication;

        const userSession = await userSessionManager.getById(userSessionId);
        if (
          userSession != null &&
          userSession.userId === userId &&
          userSession.expireTime > Date.now()
        ) {
          const user = await userManager.getById(userSession.userId);

          if (user != null) {
            if (user.isSuspended) {
              ApiError.throw(
                ApiErrorType.Forbidden,
                `User @${user.username} is currently suspended.`
              );
            }
          }

          try {
            const unlockedSession = userSessionManager.unlock(
              userSession,
              Buffer.from(userSessionKey, "base64") as any
            );

            const userAuthentication = await userAuthenticationManager.getById(
              unlockedSession.originUserAuthenticationId
            );

            if (userAuthentication != null) {
              return true;
            }
          } catch {
            //
          }
        }

        return false;
      },

      restore: async (authentication) => {
        requireAuthenticated(false);

        const { userId, userSessionId, userSessionKey } = authentication;

        const userSession = await userSessionManager.getById(userSessionId);

        if (
          userSession != null &&
          userSession.userId === userId &&
          userSession.expireTime > Date.now()
        ) {
          const user = await userManager.getById(userSession.userId);

          if (user != null) {
            if (user.isSuspended) {
              ApiError.throw(
                ApiErrorType.Forbidden,
                `User @${user.username} is currently suspended.`
              );
            }

            try {
              const unlockedSession = userSessionManager.unlock(
                userSession,
                Buffer.from(userSessionKey, "base64") as any
              );

              const userAuthentication =
                await userAuthenticationManager.getById(
                  unlockedSession.originUserAuthenticationId
                );

              if (userAuthentication != null) {
                this.#userAuthentication = userSessionManager.unlockKey(
                  unlockedSession,
                  userAuthentication
                );

                return {
                  userId: userId,
                  userSessionId: unlockedSession.id,
                  userSessionKey: Buffer.from(unlockedSession.key).toString(
                    "base64"
                  ),
                };
              }
            } catch (error) {
              // console.log(error);
              //
            }
          }
        }

        ApiError.throw(ApiErrorType.Unauthorized, "Invalid login details");
      },

      whoAmI: async () => {
        const authentication = this.#userAuthentication;

        if (authentication == null) {
          return null;
        }

        return await resolveUser(["userId", authentication.userId]);
      },

      authenticate: async (resolve, type, payload) => {
        requireAuthenticated(false);

        let user: UserResource | null = null;
        let unlockedUserAuthentication: UnlockedUserAuthentication | null =
          null;

        try {
          user = await resolveUser(resolve);
          unlockedUserAuthentication =
            await userAuthenticationManager.findByPayload(user, type, payload);
        } catch {
          unlockedUserAuthentication = null;
        }

        if (unlockedUserAuthentication == null || user == null) {
          ApiError.throw(ApiErrorType.Unauthorized, "Invalid credentials");
        }

        if (user.isSuspended) {
          ApiError.throw(ApiErrorType.Forbidden, "User is currently suspended");
        }

        const unlockedSession = await userSessionManager.create(
          unlockedUserAuthentication,
          UserSessionType.Browser
        );

        this.#userAuthentication = unlockedUserAuthentication;
        return {
          userId: user.id,
          userSessionId: unlockedSession.id,
          userSessionKey: Buffer.from(unlockedSession.key).toString("base64"),
        };
      },

      getServerStatus: async () => {
        const setupRequired = await userManager
          .count([["role", ">=", UserRole.SiteAdmin]])
          .then((result) => result === 0);

        return { setupRequired };
      },

      async register(username, firstName, middleName, lastName, password) {
        requireAuthenticated(false);

        const status = await serverFunctions.getServerStatus();
        if (!status.setupRequired) {
          ApiError.throw(
            ApiErrorType.InvalidRequest,
            "Admin user already exists"
          );
        }

        const [user] = await userManager.create(
          username,
          firstName,
          middleName,
          lastName,
          password,
          UserRole.SiteAdmin
        );

        return user;
      },

      async getUser(resolve) {
        requireAuthenticated(true);

        return await resolveUser(resolve);
      },

      listUsers: async ({ searchString: search, offset, limit } = {}) => {
        await requireRole(requireAuthenticated(true), UserRole.Member);

        const users = await userManager.read({
          search,
          offset,
          limit,
        });

        return users;
      },

      createUser: async (
        username,
        firstName,
        middleName,
        lastName,
        password,
        role
      ) => {
        await requireRole(requireAuthenticated(true), UserRole.SiteAdmin);

        const [users] = database.getManagers(UserManager);
        const [user, unlockedUserKey] = await users.create(
          username,
          firstName,
          middleName,
          lastName,
          password,
          role
        );

        return [user, unlockedUserKey, password];
      },

      updateUser: async ({ firstName, middleName, lastName, role }) => {
        const { userId } = requireAuthenticated(true);

        const user = await resolveUser(["userId", userId]);

        if (user.id !== userId) {
          ApiError.throw(ApiErrorType.Forbidden);
        }

        return await userManager.update(user, {
          firstName,
          middleName,
          lastName,
          role,
        });
      },

      setSuspend: async (id, isSuspended) => {
        await requireRole(requireAuthenticated(true), UserRole.SiteAdmin);
        const user = await resolveUser(["userId", id]);

        if (user.role >= UserRole.SiteAdmin) {
          ApiError.throw(
            ApiErrorType.InvalidRequest,
            "Cannot suspend site admin"
          );
        }

        await userManager.setSuspended(user, isSuspended);
        if (isSuspended) {
          for (const connection of this.#manager.getConnectionsFromUser(
            user.id
          )) {
            connection.#io.disconnect();
          }
        }
      },

      createFolder: async (parentFolderId, name) => {
        const authentication = requireAuthenticated(true);
        const parentFolder = await getFile(
          parentFolderId,
          authentication,
          FileAccessLevel.ReadWrite
        );

        const folder = await fileManager.create(
          authentication,
          parentFolder,
          name,
          FileType.Folder
        );

        const user = (await userManager.getById(authentication.userId))!;
        await fileLogManager.push(parentFolder, user, FileLogType.Modify);
        await fileLogManager.push(folder, user, FileLogType.Create);

        return (await fileManager.getById(folder.id))!;
      },

      scanFolder: async (folderId, sort) => {
        const authentication = requireAuthenticated(true);
        const folder = await getFile(
          folderId,
          authentication,
          FileAccessLevel.Read
        );

        const user = await resolveUser(["userId", authentication.id]);

        await fileLogManager.push(folder, user, FileLogType.Access);
        const files = await fileManager.scanFolder(folder);

        if (sort == null) {
          return files;
        }

        return await sortFiles(files, sort);
      },

      setUserAccess: async (fileId, targetUserId, level) => {
        const authentication = requireAuthenticated(true);
        const file = await getFile(fileId, authentication);
        const granterUser = await resolveUser([
          "userId",
          authentication.userId,
        ]);

        const granterAccess = await fileAccessManager.getAccessLevel(
          file,
          granterUser
        );

        if (granterAccess < FileAccessLevel.Manage) {
          ApiError.throw(ApiErrorType.Forbidden, "Insufficient access level");
        }

        const user = await resolveUser(["userId", targetUserId]);

        await fileAccessManager.setUserAccess(
          file,
          user,
          level ?? FileAccessLevel.None,
          granterUser
        );
        return level ?? FileAccessLevel.None;
      },

      getFile: async (fileId: number | null) => {
        const authentication = requireAuthenticated(true);

        const file = await getFile(
          fileId,
          authentication,
          FileAccessLevel.Read
        );

        return (await fileManager.getById(file.id))!;
      },

      adminGetFile: async (fileId: number) => {
        const authentication = requireAuthenticated(true);
        await requireRole(authentication, UserRole.SiteAdmin);

        const file = await fileManager.getById(fileId);
        if (file == null) {
          ApiError.throw(ApiErrorType.NotFound, "File not found");
        }

        return file;
      },

      adminScanFolder: async (fileId: number, sort) => {
        const authentication = requireAuthenticated(true);
        await requireRole(authentication, UserRole.SiteAdmin);

        const file = await fileManager.getById(fileId);
        if (file == null) {
          ApiError.throw(ApiErrorType.NotFound, "File not found");
        }

        if (file.type !== FileType.Folder) {
          ApiError.throw(ApiErrorType.InvalidRequest, "Not a folder");
        }

        const files = await fileManager.scanFolder(file);

        if (sort == null) {
          return files;
        }

        return await sortFiles(files, sort);
      },

      getFilePathChain: async (fileId: number) => {
        const authentication = requireAuthenticated(true);
        let file = await getFile(fileId, authentication, FileAccessLevel.Read);

        const chain: FileResource[] = [];
        while (file != null) {
          chain.unshift((await fileManager.getById(file.id))!);

          const parentFolderId = file.parentFileId;
          if (parentFolderId == null) {
            break;
          }

          try {
            file = await getFile(
              parentFolderId,
              authentication,
              FileAccessLevel.Read
            );
          } catch {
            break;
          }
        }

        return chain;
      },

      getFileSize: async (fileId, fileSnapshotId) => {
        const authentication = requireAuthenticated(true);
        const file = await getFile(
          fileId,
          authentication,
          FileAccessLevel.Read,
          FileType.File
        );

        const fileContent = await fileContentManager.getMain(file);
        const fileSnapshot =
          fileSnapshotId != null
            ? await fileSnapshotManager.getById(fileSnapshotId)
            : await fileSnapshotManager.getLatest(file, fileContent);

        if (fileSnapshot == null) {
          ApiError.throw(ApiErrorType.NotFound, "File snapshot not found");
        }

        return fileSnapshot.size;
      },

      getFileMime: async (fileId, fileSnapshotId) => {
        const authentication = requireAuthenticated(true);
        const file = await getFile(
          fileId,
          authentication,
          FileAccessLevel.Read,
          FileType.File
        );
        const fileContent = await fileContentManager.getMain(file);
        const fileSnapshot =
          fileSnapshotId != null
            ? await fileSnapshotManager.getById(fileSnapshotId)
            : await fileSnapshotManager.getLatest(file, fileContent);

        if (fileSnapshot == null) {
          ApiError.throw(ApiErrorType.NotFound, "File snapshot not found");
        }

        return await server.mimeDetector.getFileMime(
          file,
          fileContent,
          fileSnapshot
        );
      },

      getFileTime: async (fileId) => {
        const authentication = requireAuthenticated(true);
        const file = await getFile(
          fileId,
          authentication,
          FileAccessLevel.Read
        );

        if (file.type === FileType.Folder) {
          const resourceStats = (await fileManager.getStats(file.id))!;

          return {
            createTime: resourceStats.createTime,
            modifyTime: resourceStats.updateTime,
          };
        } else if (file.type === FileType.File) {
          const fileContent = await fileContentManager.getMain(file);
          const firstSnapshot = await fileSnapshotManager.getRoot(
            file,
            fileContent
          );
          const latestSnapshot = await fileSnapshotManager.getLatest(
            file,
            fileContent
          );

          return {
            createTime: firstSnapshot.createTime,
            modifyTime: latestSnapshot.createTime,
          };
        }

        throw ApiError.throw(ApiErrorType.InvalidRequest);
      },

      listFileViruses: async (fileId, fileSnapshotId) => {
        const authentication = requireAuthenticated(true);
        const file = await getFile(
          fileId,
          authentication,
          FileAccessLevel.Read,
          FileType.File
        );
        const fileContent = await fileContentManager.getMain(file);
        const fileSnapshot =
          fileSnapshotId != null
            ? await fileSnapshotManager.getById(fileSnapshotId)
            : await fileSnapshotManager.getLatest(file, fileContent);

        if (fileSnapshot == null) {
          ApiError.throw(ApiErrorType.NotFound, "File snapshot not found");
        }

        return await server.virusScanner.scan(
          file,
          fileContent,
          fileSnapshot,
          false
        );
      },

      listFileAccess: async (fileId, offset, limit) => {
        const authentication = requireAuthenticated(true);
        const file = await getFile(
          fileId,
          authentication,
          FileAccessLevel.Read
        );

        let fileAccesses: FileAccessResource[];
        if (file.ownerUserId !== authentication.userId) {
          fileAccesses = await fileAccessManager.read({
            where: [
              ["fileId", "=", file.id],
              ["userId", "=", authentication.userId],
            ],
            orderBy: [["level", true]],
            offset,
            limit,
          });
        } else {
          fileAccesses = await fileAccessManager.read({
            where: [["fileId", "=", file.id]],
            orderBy: [["level", true]],
            offset,
            limit,
          });
        }

        return fileAccesses;
      },

      listFileSnapshots: async (fileId) => {
        const authentication = requireAuthenticated(true);
        const file = await getFile(
          fileId,
          authentication,
          FileAccessLevel.Read,
          FileType.File
        );

        const fileContent = await fileContentManager.getMain(file);
        return await fileSnapshotManager.list(file, fileContent);
      },

      listFileLogs: async (fileId, userIds, types, offset, limit) => {
        const authentication = requireAuthenticated(true);
        const logs: FileLogResource[] = [];

        const file = await getFile(
          fileId,
          authentication,
          FileAccessLevel.Read
        );

        for await (const log of fileLogManager.readStream({
          where: [
            ["targetFileId", "=", file.id],
            userIds != null ? ["actorUserId", "in", userIds] : null,
            types != null ? ["type", "in", types] : null,
          ],
          offset,
          limit,
          orderBy: [["id", true]],
        })) {
          ``;
          logs.push(log);
        }

        return logs;
      },

      adminListFileLogs: async (fileIds, userIds, types, offset, limit) => {
        const authentication = requireAuthenticated(true);
        await requireRole(authentication, UserRole.SiteAdmin);

        const logs: FileLogResource[] = [];

        for await (const log of fileLogManager.readStream({
          where: [
            fileIds != null ? ["targetFileId", "in", fileIds] : null,
            userIds != null ? ["actorUserId", "in", userIds] : null,
            types != null ? ["type", "in", types] : null,
          ],
          offset,
          limit,
          orderBy: [["id", true]],
        })) {
          logs.push(log);
        }

        return logs;
      },

      getMyAccess: async (fileId) => {
        const authentication = requireAuthenticated(true);

        const me = await currentUser(authentication);
        const file = await getFile(
          fileId,
          authentication,
          FileAccessLevel.None
        );

        if (file.ownerUserId === me.id) {
          return { level: FileAccessLevel.Full, access: null };
        }

        const fileAccesses = await fileAccessManager.read({
          where: [
            ["userId", "=", me.id],
            ["fileId", "=", file.id],
          ],
        });

        const access = fileAccesses.reduce(
          (highest: FileAccessResource | null, current) => {
            if (highest == null) {
              return current;
            }

            if (highest.level > current.level) {
              return highest;
            }

            return current;
          },
          null
        );

        return {
          access,
          level: access?.level ?? FileAccessLevel.None,
        };
      },

      openNewFile: async (parentFolderId, name) => {
        const authentication = requireAuthenticated(true);
        const parentFolder = await getFile(
          parentFolderId,
          authentication,
          FileAccessLevel.ReadWrite
        );

        const file = await this.#manager.server.fileManager.openNewFile(
          this,
          authentication,
          parentFolder,
          name
        );

        return file;
      },

      openFile: async (fileId) => {
        const authentication = requireAuthenticated(true);
        const file = await getFile(
          fileId,
          authentication,
          FileAccessLevel.None
        );

        return await this.#manager.server.fileManager.openFile(
          this,
          authentication,
          file
        );
      },

      openFileThumbnail: async (fileId, snapshotId) => {
        const authentication = requireAuthenticated(true);
        const file = await getFile(
          fileId,
          authentication,
          FileAccessLevel.None
        );

        const fileHandleId =
          await this.#manager.server.fileManager.openFileThumbnail(
            this,
            authentication,
            file
          );

        if (fileHandleId == null) {
          ApiError.throw(ApiErrorType.NotFound, "No thumbnail available");
        }

        return fileHandleId;
      },

      truncateFile: async (handleId, length) => {
        const authentication = requireAuthenticated(true);

        await this.#manager.server.fileManager.truncate(
          this,
          authentication,
          handleId,
          length
        );
      },

      readFile: async (handleId, position, length) => {
        const authentication = requireAuthenticated(true);

        return await this.#manager.server.fileManager.read(
          this,
          authentication,
          handleId,
          position,
          length
        );
      },

      writeFile: async (handleId, position, data) => {
        const authentication = requireAuthenticated(true);

        await this.#manager.server.fileManager.write(
          this,
          authentication,
          handleId,
          position,
          data
        );
      },

      closeFile: async (handleId) => {
        this.#manager.server.fileManager.close(this, handleId);
      },

      moveFile: async (fileIds, toParentId) => {
        const authentication = requireAuthenticated(true);

        for (const fileId of fileIds) {
          const file = await getFile(
            fileId,
            authentication,
            FileAccessLevel.None
          );

          const toParent = await getFile(
            toParentId,
            authentication,
            FileAccessLevel.ReadWrite,
            FileType.Folder
          );

          await fileManager.move(file, toParent);
        }
      },

      getFileHandleSize: async (handleId) => {
        const authentication = requireAuthenticated(true);

        return await this.#manager.server.fileManager.length(
          this,
          authentication,
          handleId
        );
      },

      copyFile: async (fileIds, toParentId, fileSnapshotId) => {
        const authentication = requireAuthenticated(true);

        for (const fileId of fileIds) {
          const file = await getFile(
            fileId,
            authentication,
            FileAccessLevel.None
          );

          const toParent = await getFile(
            toParentId,
            authentication,
            FileAccessLevel.ReadWrite,
            FileType.Folder
          );

          const copy = async (
            sourceFile: UnlockedFileResource,
            destinationFile: UnlockedFileResource
          ) => {
            if (sourceFile.type === FileType.File) {
              const fileContent = await fileContentManager.getMain(file);
              const fileSnapshot = await fileSnapshotManager.getLatest(
                file,
                fileContent
              );

              const newFile = await fileManager.create(
                authentication,
                toParent,
                file.name,
                FileType.File
              );
              const newFileContent = await fileContentManager.getMain(newFile);
              const newFileSnapshot = await fileSnapshotManager.getRoot(
                newFile,
                newFileContent
              );

              for (
                let position = 0;
                position < fileSnapshot.size;
                position += fileIoSize
              ) {
                const buffer = await fileDataManager.readData(
                  file,
                  fileContent,
                  fileSnapshot,
                  position,
                  fileIoSize
                );

                // console.log(buffer);

                await fileDataManager.writeData(
                  newFile,
                  newFileContent,
                  newFileSnapshot,
                  position,
                  buffer
                );
              }
            } else if (sourceFile.type === FileType.Folder) {
              let newFolder: UnlockedFileResource;

              {
                const newFolderFind = await fileManager.getByName(
                  destinationFile,
                  sourceFile.name
                );

                if (newFolderFind == null) {
                  newFolder = await fileManager.create(
                    authentication,
                    destinationFile,
                    sourceFile.name,
                    FileType.Folder
                  );
                } else {
                  newFolder = await fileManager.unlock(
                    newFolderFind,
                    authentication,
                    FileAccessLevel.ReadWrite
                  );
                }
              }

              for (const file of await fileManager
                .scanFolder(sourceFile)
                .then((files) =>
                  Promise.all(
                    files.map((file) =>
                      fileManager.unlock(
                        file,
                        authentication,
                        FileAccessLevel.Read
                      )
                    )
                  )
                )) {
                await copy(file, newFolder);
              }
            }
          };

          await copy(file, toParent);
        }
      },

      trashFile: async (fileIds) => {
        const authentication = requireAuthenticated(true);

        for (const fileId of fileIds) {
          const file = await getFile(
            fileId,
            authentication,
            FileAccessLevel.Manage
          );

          if (file.deleted) {
            return;
          }

          await fileManager.trash(file);
        }
      },

      purgeFile: async (fileIds) => {
        const authentication = requireAuthenticated(true);

        for (const fileId of fileIds) {
          const file = await getFile(
            fileId,
            authentication,
            FileAccessLevel.Manage
          );

          if (!file.deleted) {
            return;
          }

          await fileManager.delete(file);
        }
      },

      restoreFile: async (fileIds, newParentId) => {
        const authentication = requireAuthenticated(true);

        for (const fileId of fileIds) {
          const file = await getFile(
            fileId,
            authentication,
            FileAccessLevel.Manage
          );

          await fileManager.untrash(
            authentication,
            file,
            newParentId != null
              ? await getFile(
                  newParentId,
                  authentication,
                  FileAccessLevel.ReadWrite,
                  FileType.Folder
                )
              : undefined
          );
        }
      },

      listTrashedFiles: async (offset, limit) => {
        const authentication = requireAuthenticated(true);
        const user = await currentUser(authentication);

        return await fileManager.listTrashed(user, { offset, limit });
      },

      listSharedFiles: async (targetUserId, sharerUserId, offset, limit) => {
        const authentication = requireAuthenticated(true);

        const targetUser =
          targetUserId != null
            ? await resolveUser(["userId", targetUserId])
            : null;

        const sharerUser =
          sharerUserId != null
            ? await Promise.all(
                sharerUserId.map((userId) => resolveUser(["userId", userId]))
              )
            : null;

        const fileAccesses = await fileAccessManager.read({
          where: [
            ["userId", "=", authentication.userId],
            ["level", ">=", FileAccessLevel.Read],
          ],
          offset,
          limit,
        });

        return fileAccesses;
      },

      listStarredFiles: async (fileId, userId, offset, limit) => {
        const authentication = requireAuthenticated(true);

        const file =
          fileId != null
            ? await getFile(fileId, authentication, FileAccessLevel.Read)
            : null;

        const user =
          userId != null ? await resolveUser(["userId", userId]) : null;

        return await fileStarManager.read({
          where: [
            file != null ? ["fileId", "=", file.id] : null,
            user != null ? ["userId", "=", user.id] : null,
          ],

          offset,
          limit,
        });
      },

      isFileStarred: async (fileId) => {
        const authentication = requireAuthenticated(true);

        const file = await getFile(
          fileId,
          authentication,
          FileAccessLevel.Read
        );

        return (await fileStarManager.count([["fileId", "=", file.id]])) !== 0;
      },

      setFileStar: async (fileId, starred) => {
        const authentication = requireAuthenticated(true);

        const user = await resolveUser(["userId", authentication.userId]);

        const file = await getFile(
          fileId,
          authentication,
          FileAccessLevel.Read
        );

        await fileStarManager.setStar(user, file, starred);
        return starred;
      },

      createNews: async (news) => {
        await requireRole(requireAuthenticated(true), UserRole.SiteAdmin);
        newsArray.push(news);
      },

      deleteNews: async (index) => {
        await requireRole(requireAuthenticated(true), UserRole.SiteAdmin);
        newsArray.splice(index, 1);
      },

      getNewsCount: async () => newsArray.length,

      getNews: async (index) => newsArray[index],
    };

    return serverFunctions;
  }
}
