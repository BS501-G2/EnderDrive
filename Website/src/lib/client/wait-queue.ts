import { CancellationToken, createCancellationToken, PromiseCompletionSource, waitAsync } from './promise-source';

export class WaitQueue<T> {
	public constructor(capacity: number | null) {
		this.#capacity = capacity;
		this.#backlog = [];
		this.#insert = [];
		this.#take = [];
	}

	readonly #capacity: number | null;
	readonly #backlog: T[];
	readonly #insert: PromiseCompletionSource<PromiseCompletionSource<T>>[];
	readonly #take: PromiseCompletionSource<T>[];

	public get count() {
		return this.#backlog.length + this.#insert.length;
	}

	public async dequeue(cancellationToken: CancellationToken): Promise<T> {
		const source: PromiseCompletionSource<T> = new PromiseCompletionSource();

		while (true) {
			if (cancellationToken.requested) {
				source.cancel(cancellationToken);
				break;
			}

			const backlog = this.#backlog.shift();

			if (backlog != null) {
				source.resolve(backlog);
				break;
			}

			const insert = this.#insert.shift();
			if (insert != null) {
				if (!insert.resolve(source)) {
					continue;
				}

				break;
			}

			this.#take.push(source);
			break;
		}

		return await waitAsync(source.promise, cancellationToken);
	}

	public async enqueue(
		item: T,
		cancellationToken: CancellationToken = createCancellationToken(false)
	): Promise<void> {
		let source: PromiseCompletionSource<PromiseCompletionSource<T>> | null = null;

		while (true) {
			cancellationToken.throwIfCancellationRequested();

			if (
				this.#insert.length === 0 &&
				this.#take.length === 0 &&
				(this.#capacity === null || this.#backlog.length < this.#capacity)
			) {
				this.#backlog.push(item);
				break;
			}

			let take: PromiseCompletionSource<T> | undefined;
			if (this.#insert.length === 0 && (take = this.#take.shift()) != null) {
				if (!take.resolve(item)) {
					continue;
				}

				break;
			}

			source = new PromiseCompletionSource();
			this.#insert.push(source);
			break;
		}

		await waitAsync(
			source?.promise.then((source) => {
				source.resolve(item);
			}) ?? Promise.resolve(),
			cancellationToken
		);
	}
}
