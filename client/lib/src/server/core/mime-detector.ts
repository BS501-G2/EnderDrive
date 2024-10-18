import {
  fileBufferSize,
  Service,
  ServiceGetDataCallback,
  ServiceReadyCallback,
  ServiceSetDataCallback,
} from "../../shared.js";
import { FileContentResource } from "../db/file-content.js";
import { FileDataManager } from "../db/file-data.js";
import { FileMimeManager } from "../db/file-mime.js";
import { FileSnapshotResource } from "../db/file-snapshot.js";
import { UnlockedFileResource } from "../db/file.js";
import { Server } from "./server.js";

type Magic = any;

export interface MimeDetectorData {
  mimeDetector: Magic;
  descriptionDetector: Magic;
}

export type MimeDetectorOptions = [];

export class MimeDetector extends Service<
  MimeDetectorData,
  MimeDetectorOptions
> {
  public constructor(server: Server) {
    let getData: ServiceGetDataCallback<MimeDetectorData> = null as never;

    super((func) => (getData = func), server);

    this.#server = server;
    this.#getData = getData;
  }

  readonly #server: Server;
  readonly #getData: ServiceGetDataCallback<MimeDetectorData>;

  get #data() {
    return this.#getData();
  }

  get #mimeDetector() {
    return this.#data.mimeDetector;
  }

  get #descriptionDetector() {
    return this.#data.descriptionDetector;
  }

  get #database() {
    return this.#server.database;
  }

  async run(
    setData: ServiceSetDataCallback<MimeDetectorData>,
    onReady: ServiceReadyCallback
  ): Promise<void> {
    // const descriptionDetector = new Magic();
    // const mimeDetector = new Magic(
    //   mmm.MAGIC_MIME_TYPE | mmm.MAGIC_MIME_ENCODING
    // );

    const descriptionDetector = {};
    const mimeDetector = {};

    setData({ mimeDetector, descriptionDetector });

    await new Promise<void>(onReady);
  }

  #getBufferMime(
    buffer: Buffer,
    description: boolean = false
  ): Promise<string> {
    return Promise.resolve(
      description ? "Binary Data" : "application/octet-stream"
    );

    // return new Promise<string>((resolve, reject) => {
    //   (description ? this.#descriptionDetector : this.#mimeDetector).detect(
    //     buffer,
    //     (error, result) => {
    //       if (error != null) {
    //         reject(error);
    //       } else {
    //         resolve(Array.isArray(result) ? result[0] : result);
    //       }
    //     }
    //   );
    // });
  }

  async getFileMime(
    file: UnlockedFileResource,
    fileContent: FileContentResource,
    fileSnapshot: FileSnapshotResource
  ): Promise<[mime: string, description: string]> {
    const [fileMimeManager, fileDataManager] = this.#database.getManagers(
      FileMimeManager,
      FileDataManager
    );

    let mime = await fileMimeManager.getMime(file, fileContent, fileSnapshot);

    if (mime != null) {
      return mime;
    }

    const firstBuffer = Buffer.from(
      await fileDataManager.readData(
        file,
        fileContent,
        fileSnapshot,
        0,
        1024 * 1024
      )
    );

    mime = await Promise.all([
      this.#getBufferMime(firstBuffer, false).then(
        (mime) => mime.split(";")[0]
      ),
      this.#getBufferMime(firstBuffer, true).then((description) =>
        description.split(",")[0].trim()
      ),
    ] as [mime: Promise<string>, description: Promise<string>]);

    return await fileMimeManager.setMime(
      file,
      fileContent,
      fileSnapshot,
      ...mime
    );
  }
}
