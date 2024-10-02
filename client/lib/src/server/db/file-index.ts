import { Knex } from "knex";
import { Resource, ResourceManager } from "../resource.js";
import { FileManager, FileResource } from "./file.js";
import { FileContentManager, FileContentResource } from "./file-content.js";
import { FileSnapshotManager, FileSnapshotResource } from "./file-snapshot.js";
import { Database } from "../database.js";

export interface FileIndexResource
  extends Resource<FileIndexResource, FileIndexManager> {
  fileId: number;
  fileContentId: number;
  fileSnapshotId: number;

  token: string;
}

export class FileIndexManager extends ResourceManager<
  FileIndexResource,
  FileIndexManager
> {
  public constructor(
    db: Database,
    init: (init: (version?: number) => Promise<void>) => void
  ) {
    super(db, init, "FileIndex", 1);
  }

  protected upgrade(table: Knex.AlterTableBuilder, version: number): void {
    if (version < 1) {
      table
        .integer("fileId")
        .notNullable()
        .references("id")
        .inTable(this.getManager(FileManager).recordTableName)
        .onDelete("cascade");

      table
        .integer("fileContentId")
        .notNullable()
        .references("id")
        .inTable(this.getManager(FileContentManager).recordTableName)
        .onDelete("cascade");

      table
        .integer("fileSnapshotId")
        .notNullable()
        .references("id")
        .inTable(this.getManager(FileSnapshotManager).recordTableName)
        .onDelete("cascade");

      table.string("token").notNullable();
    }
  }

  public async pushToken(
    file: FileResource,
    fileContent: FileContentResource,
    fileSnapshotResource: FileSnapshotResource,
    token: string
  ) {
    await this.insert({
      fileId: file.id,
      fileContentId: fileContent.id,
      fileSnapshotId: fileSnapshotResource.id,
      token,
    });
  }

  public async searchByToken(token: string) {
    for (const a of await this.read({
      where: [["token", "like", `%${token}%`]],
    })) {
      const file = (await this.getManager(FileManager).getById(a.fileId))!;
      const fileContent = (await this.getManager(FileContentManager).getById(
        a.fileContentId
      ))!;

      return a;
    }
  }
}
