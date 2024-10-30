const CancellationTokenBindingSymbol: unique symbol = Symbol('Cancellation Token Binding');
let cancellationTokenCreationPass: boolean = false;

interface CancellationTokenBinding {
	registrations?: (() => void)[];
}

export class CancellationTokenSource implements Disposable {
	public constructor(links: CancellationToken[] = []) {
		this[CancellationTokenBindingSymbol] = {
			registrations: []
		};

		this.#links = [];

		for (const link of links) {
			this.#links.push(
				link.register(() => {
					this.cancel();
				})
			);
		}
	}

	[Symbol.dispose](): void {
		// this.cancel();

		for (const link of this.#links) {
			link();
		}

		this.#links.splice(0, this.#links.length);
	}

	readonly #links: (() => void)[];
	readonly [CancellationTokenBindingSymbol]: CancellationTokenBinding;

	public get token(): CancellationToken {
		return createCancellationTokenInternal(this[CancellationTokenBindingSymbol]);
	}

	public cancel(): void {
		const binding = this[CancellationTokenBindingSymbol];
		if (binding.registrations == null) {
			return;
		}

		const registrations = binding.registrations;
		delete binding.registrations;

		if (registrations == null) {
			return;
		}

		for (const registration of registrations) {
			registration();
		}
	}
}

function createCancellationTokenInternal(binding: CancellationTokenBinding) {
	try {
		cancellationTokenCreationPass = true;

		return new CancellationToken(binding);
	} finally {
		cancellationTokenCreationPass = false;
	}
}

export function createCancellationToken(cancelled: boolean) {
	const source = new CancellationTokenSource();

	if (cancelled) {
		source.cancel();
	}

	return source.token;
}

export class CancellationToken {
	public constructor(binding: CancellationTokenBinding) {
		if (!cancellationTokenCreationPass) {
			throw new Error('Invalid constructor');
		}

		this[CancellationTokenBindingSymbol] = binding;
	}

	readonly [CancellationTokenBindingSymbol]: CancellationTokenBinding;

	public get requested(): boolean {
		return this[CancellationTokenBindingSymbol].registrations == null;
	}

	public register(callback: () => void): () => void {
		const registration = () => {
			callback();
		};

		const registrations = this[CancellationTokenBindingSymbol].registrations;
		if (registrations == null) {
			return () => {};
		}

		registrations.push(registration);

		return () => {
			const index = registrations.indexOf(registration);
			if (index >= 0) {
				registrations.splice(index, 1);
			}
		};
	}

	public throwIfCancellationRequested() {
		if (this[CancellationTokenBindingSymbol] == null) {
			throw new OperationCancelledError(this);
		}
	}

	public async getPromise(): Promise<void> {
		const source = new PromiseCompletionSource();

		if (this.requested) {
			source.cancel(this);

			return source.promise;
		} else {
			const cancellationTokenRegistration = this.register(() => source.cancel(this));

			try {
				await source.promise;
			} finally {
				cancellationTokenRegistration();
			}
		}
	}
}

export class OperationCancelledError extends Error {
	public constructor(cancellationToken: CancellationToken, options?: ErrorOptions) {
		super('Operation cancelled.', options);

		this[CancellationTokenBindingSymbol] = cancellationToken[CancellationTokenBindingSymbol];
	}

	readonly [CancellationTokenBindingSymbol]: CancellationTokenBinding;

	get cancellationToken() {
		return createCancellationTokenInternal(this[CancellationTokenBindingSymbol]);
	}
}

function getBinding(cancellationToken: CancellationToken | CancellationTokenSource) {
	return cancellationToken[CancellationTokenBindingSymbol];
}

export type CancellationTokenLike = CancellationToken | CancellationTokenSource;

export function isEqual(left: CancellationTokenLike, right: CancellationTokenLike) {
	return getBinding(left) == getBinding(right);
}

export class PromiseCompletionSource<T = void> implements PromiseLike<T> {
	public constructor() {
		this.#initialized = false;
		this.#advancedResult = null;
		this.#promise = new Promise<T>((resolve, reject) => {
			this.#initialized = true;
			this.#actions = { resolve, reject };

			if (this.#advancedResult != null) {
				if (this.#advancedResult[0] === 1) {
					resolve(this.#advancedResult[1]);
				} else if (this.#advancedResult[0] === 2) {
					reject(this.#advancedResult[2]);
				}

				this.#advancedResult = null;
			}
		});
	}

	#promise: Promise<T>;

	#initialized: boolean;
	#actions: {
		resolve: (value: T) => void;
		reject: (error: Error) => void;
	} | null = null;
	#advancedResult: [1, T, null] | [2, null, Error] | null;

	public get promise(): Promise<T> {
		return this.#promise;
	}

	get then() {
		return this.#promise.then.bind(this.#promise);
	}

	public resolve(value: T): boolean {
		if (!this.#initialized) {
			this.#advancedResult = [1, value, null];
		}

		if (this.#actions != null) {
			this.#actions.resolve(value);
			this.#actions = null;

			return true;
		}

		return false;
	}

	public reject(error: Error): boolean {
		if (!this.#initialized) {
			this.#advancedResult = [2, null, error];
		}

		if (this.#actions != null) {
			this.#actions.reject(error);
			this.#actions = null;

			return true;
		}

		return false;
	}

	public cancel(cancellationToken: CancellationToken): boolean {
		if (this.#actions != null) {
			this.#actions.reject(new TaskCancelledException(cancellationToken));
			this.#actions = null;

			return true;
		}

		return false;
	}
}

export class TaskCancelledException extends OperationCancelledError {
	public constructor(cancellationToken: CancellationToken, options?: ErrorOptions) {
		super(cancellationToken, options);
	}
}

export async function delay(time: number, cancellationToken?: CancellationToken): Promise<void> {
	const source = new PromiseCompletionSource();

	const timeout: NodeJS.Timeout | null =
		time >= 0 ? setTimeout(() => source.resolve(), time) : null;

	const cancellationTokenRegistration = cancellationToken?.register(() => {
		if (timeout != null) {
			clearTimeout(timeout);
		}

		source.cancel(cancellationToken);
	});

	try {
		return await source.promise;
	} finally {
		cancellationTokenRegistration?.();
	}
}

export async function waitAsync<T>(
	task: Promise<T>,
	cancellationToken: CancellationToken = createCancellationToken(false)
) {
	const source = new PromiseCompletionSource<T>();

	task.then(source.resolve.bind(source), source.reject.bind(source));
	const cancellationTokenRegistration = cancellationToken.register(() => {
		source.cancel(cancellationToken);
	});

	try {
		await source;
	} finally {
		cancellationTokenRegistration();
	}
}

export function waitAny<T>(promises: Promise<T>[], cancellationToken: CancellationToken) {
	const source = new PromiseCompletionSource<Promise<T>>();

	for (const promise of promises) {
		void (async () => {
			try {
				await promise;
			} catch {
				//
			} finally {
				source.resolve(promise);
			}
		})();
	}

	return waitAsync(source.promise, cancellationToken);
}

export function waitAll<T>(promises: Promise<T>[], cancellationToken: CancellationToken) {
	const source = new PromiseCompletionSource<Promise<T>[]>();

	const promisesCopy: Promise<T>[] = [...promises];
	const boundPromises: Promise<T>[] = [];

	for (const promise of promises) {
		boundPromises.push(promise);

		void (async () => {
			try {
				await promise;
			} catch {
				//
			} finally {
				const index = boundPromises.indexOf(promise);
				if (index >= 0) {
					boundPromises.splice(index, 1);
				}

				if (boundPromises.length === 0) {
					source.resolve(promisesCopy);
				}
			}
		})();
	}

	return waitAsync(source.promise, cancellationToken);
}
