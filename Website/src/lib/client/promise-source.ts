export class PromiseSource<T> {
	public constructor() {
		this.#promise = new Promise<T>((resolve, reject) => {
			this.#resolve = resolve;
			this.#reject = reject;
		});
	}

	#promise: Promise<T>;
	#resolve: ((value: T) => void) | null = null;
	#reject: ((error: Error) => void) | null = null;

	public get promise(): Promise<T> {
		return this.#promise;
	}

	public resolve(value: T): boolean {
		if (this.#resolve !== null) {
			this.#resolve(value);
			this.#resolve = null;

			return true;
		}

		return false;
	}

	public reject(error: Error): boolean {
		if (this.#reject !== null) {
			this.#reject(error);
			this.#reject = null;

			return true;
		}

		return false;
	}
}
