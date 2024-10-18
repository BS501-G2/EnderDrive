import { WaitQueue } from '$lib/client/wait-queue';

export abstract class HybridWebSocketStreamEntry {}

export class HybridWebSocketFeedEntry extends HybridWebSocketStreamEntry {
	public constructor(buffer: Uint8Array) {
		super();

		this.buffer = buffer;
	}

	public readonly buffer: Uint8Array;
}

export class HybridWebSocketDoneEntry extends HybridWebSocketStreamEntry {}

export class HybridWebSocketAbortEntry extends HybridWebSocketStreamEntry {}

export enum RedirectMode {
	Error
}

export class HybridWebSocketRedirect extends HybridWebSocketStreamEntry {
	public constructor(mode: RedirectMode, stream: HybridWebSocketStream) {
		super();

		this.mode = mode;
		this.stream = stream;
	}

	public readonly mode: RedirectMode;
	public readonly stream: HybridWebSocketStream;
}

export class HybridWebSocketState {}

export class HybridWebSocketFeedState extends HybridWebSocketState {}

export class HybridWebSocketDoneState extends HybridWebSocketState {}

export class HybridWebSocketAbortState extends HybridWebSocketState {
	public constructor(error?: Error) {
		super();

		this.error = error;
	}

	public readonly error?: Error;
}

export class HybridWebSocketRedirectState extends HybridWebSocketState {
	public constructor(mode: RedirectMode, stream: HybridWebSocketStream) {
		super();

		this.mode = mode;
		this.stream = stream;
	}

	public readonly mode: RedirectMode;
	public readonly stream: HybridWebSocketStream;
}

export class HybridWebSocketStream {
	public constructor() {
		this.#waitQueue = new WaitQueue<HybridWebSocketStreamEntry>(null);
		this.#state = new HybridWebSocketFeedState();
	}

	readonly #waitQueue: WaitQueue<HybridWebSocketStreamEntry>;
	#state: HybridWebSocketState;

	#ensureFeedState() {
		if (this.#state instanceof HybridWebSocketFeedState) {
			return;
		}

		if (this.#state instanceof HybridWebSocketAbortState && this.#state.error != null) {
			throw this.#state.error;
		}

		throw new Error('Stream is not in a feed state.');
	}

	public async push(buffer: Uint8Array) {
		const maxSize: number = 1024 * 8;

		if (buffer.length > maxSize) {
			for (let index = 0; index < buffer.length; index += maxSize) {
				await this.push(buffer.slice(index, Math.min(buffer.length, index + maxSize)));
			}

			return this;
		}

		this.#ensureFeedState();
		await this.#waitQueue.enqueue(new HybridWebSocketFeedEntry(buffer));

		return this;
	}

	public async finish() {
		this.#ensureFeedState();
		await this.#waitQueue.enqueue(new HybridWebSocketDoneEntry());

		return this;
	}

	public async abort(error?: Error) {
		this.#ensureFeedState();
		await this.#waitQueue.enqueue(new HybridWebSocketAbortEntry());

		this.#state = new HybridWebSocketAbortState(error);
		return this;
	}

	public async redirect(mode: RedirectMode, stream: HybridWebSocketStream | null = null) {
		this.#ensureFeedState();
		stream ??= new HybridWebSocketStream();
		await this.#waitQueue.enqueue(new HybridWebSocketRedirect(mode, stream));
		this.#state = new HybridWebSocketRedirectState(mode, stream);

		return this;
	}

	public async shift(): Promise<Uint8Array | null> {
		if (this.#waitQueue.count === 0 && !(this.#state instanceof HybridWebSocketFeedEntry)) {
			if (this.#state instanceof HybridWebSocketAbortState && this.#state.error != null) {
				throw this.#state.error;
			}

			throw new Error('Stream is not in a feed state.');
		}

		const item = await this.#waitQueue.dequeue();

		if (item instanceof HybridWebSocketFeedEntry) {
			return item.buffer;
		} else if (item instanceof HybridWebSocketDoneEntry) {
			return null;
		} else if (item instanceof HybridWebSocketAbortEntry) {
			throw new Error();
		} else if (item instanceof HybridWebSocketRedirect) {
			throw new HybridWebSocketStreamError(item.mode, item.stream);
		}

		throw new Error();
	}
}

export class HybridWebSocketStreamError extends Error {
	public constructor(mode: RedirectMode, stream: HybridWebSocketStream) {
		super('Stream error');

		this.mode = mode;
		this.stream = stream;
	}

	public readonly mode: RedirectMode;
	public readonly stream: HybridWebSocketStream;
}
