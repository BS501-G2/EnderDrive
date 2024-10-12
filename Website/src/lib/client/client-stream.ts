import { WaitQueue } from '$lib/wait-queue';

export abstract class ClientStreamState {
	public constructor() {}
}

export const CLIENT_STREAM_REDIRECT_MODE_ERROR = 0b0001;
export type ClientStreamRedirectMode = typeof CLIENT_STREAM_REDIRECT_MODE_ERROR;

export class ClientStreamFeedState extends ClientStreamState {
	public constructor() {
		super();
	}
}

export class ClientStreamDoneState extends ClientStreamState {
	public constructor() {
		super();
	}
}

export class ClientStreamAbortState extends ClientStreamState {
	public constructor(error?: Error) {
		super();

		this.#error = error;
	}

	readonly #error?: Error;

	public get error(): Error | null {
		return this.#error ?? null;
	}
}

export class ClientStreamRedirectState extends ClientStreamState {
	public constructor(mode: ClientStreamRedirectMode, stream: ClientStream) {
		super();

		this.#mode = mode;
		this.#stream = stream;
	}

	readonly #mode: ClientStreamRedirectMode;
	readonly #stream: ClientStream;

	public get mode() {
		return this.#mode;
	}

	public get stream() {
		return this.#stream;
	}
}

export abstract class ClientStreamEntry {
	public constructor() {}
}

export class ClientStreamEntryFeed extends ClientStreamEntry {
	public constructor(buffer: Uint8Array) {
		super();

		this.#buffer = buffer;
	}

	readonly #buffer: Uint8Array;

	public get buffer() {
		return this.#buffer;
	}
}

export class ClientStreamDoneEntry extends ClientStreamEntry {
	public constructor() {
		super();
	}
}

export class ClientStreamAbortEntry extends ClientStreamEntry {
	public constructor() {
		super();
	}
}

export class ClientStreamRedirectEntry extends ClientStreamEntry {
	public constructor(mode: ClientStreamRedirectMode, stream: ClientStream) {
		super();

		this.#mode = mode;
		this.#stream = stream;
	}

	readonly #mode: ClientStreamRedirectMode;
	readonly #stream: ClientStream;

	public get mode() {
		return this.#mode;
	}

	public get stream() {
		return this.#stream;
	}
}

export class ClientStream {
	public constructor() {
		this.#waitQueue = new WaitQueue(null);
		this.#state = new ClientStreamFeedState();
	}

	readonly #waitQueue: WaitQueue<ClientStreamEntry>;
	#state: ClientStreamState;

	#ensureFeedState() {
		if (!(this.#state instanceof ClientStreamFeedState)) {
			if (this.#state instanceof ClientStreamAbortState) {
				throw this.#state.error;
			}

			throw new Error('Invalid state');
		}
	}

	public async push(buffer: Uint8Array): Promise<ClientStream> {
		const maxSize = 1024 * 8;
		if (buffer.length < maxSize) {
			for (let offset = 0; offset < buffer.length; offset += maxSize) {
				await this.push(buffer.slice(offset, Math.min(offset + maxSize, buffer.length)));
			}
		}

		this.#ensureFeedState();

		await this.#waitQueue.enqueue(new ClientStreamEntryFeed(buffer));
		return this;
	}

	public async finish(): Promise<ClientStream> {
		this.#ensureFeedState();

		await this.#waitQueue.enqueue(new ClientStreamDoneEntry());
		this.#state = new ClientStreamDoneState();
		return this;
	}

	public async abort(error?: Error): Promise<ClientStream> {
		this.#ensureFeedState();

		await this.#waitQueue.enqueue(new ClientStreamAbortEntry());
		this.#state = new ClientStreamAbortState(error);
		return this;
	}

	public async redirect(
		mode: ClientStreamRedirectMode,
		stream?: ClientStream | null
	): Promise<ClientStream> {
		this.#ensureFeedState();

		stream ??= new ClientStream();

		await this.#waitQueue.enqueue(new ClientStreamRedirectEntry(mode, stream));
		this.#state = new ClientStreamRedirectState(mode, stream);

		return stream;
	}

	public async shift(): Promise<Uint8Array | null> {
		const entry = await this.#waitQueue.dequeue();
		if (entry instanceof ClientStreamEntryFeed) {
			return entry.buffer;
		} else if (entry instanceof ClientStreamDoneEntry) {
			return null;
		} else if (entry instanceof ClientStreamAbortEntry) {
			throw new Error('Aborted');
		} else if (entry instanceof ClientStreamRedirectEntry) {
			return entry.stream.shift();
		} else {
			throw new Error('Invalid state');
		}
	}
}
