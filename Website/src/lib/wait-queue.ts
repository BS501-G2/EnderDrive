import { PromiseSource } from './promise-source';

export class WaitQueue<T> {
	public constructor(capacity: number | null) {
		this.#capacity = capacity;
		this.#backlog = [];
		this.#insert = [];
		this.#take = [];
	}

	readonly #capacity: number | null;
	readonly #backlog: T[];
	readonly #insert: PromiseSource<PromiseSource<T>>[];
	readonly #take: PromiseSource<T>[];

	public async dequeue(): Promise<T> {
		const source: PromiseSource<T> = new PromiseSource();

		while (true) {
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

		return await source.promise;
	}

	public async enqueue(item: T): Promise<void> {
		let source: PromiseSource<PromiseSource<T>> | null = null;

		while (true) {
			if (
				this.#insert.length === 0 &&
				this.#take.length === 0 &&
				(this.#capacity === null || this.#backlog.length < this.#capacity)
			) {
				this.#backlog.push(item);
				break;
			}

			if (this.#insert.length === 0) {
				const take = this.#take.shift();

				if (take != null) {
					if (!take.resolve(item)) {
						continue;
					}

					break;
				}
			}

			source = new PromiseSource();
			this.#insert.push(source);
			break;
		}

		if (source != null) {
			await source.promise.then((source) => source.resolve(item));
		}
	}
}
