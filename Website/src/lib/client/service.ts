// import type { CancellationTokenSource } from './cancellation-token';

// export interface ServiceContext<T> {
// 	promise: Promise<void>;
// 	cancellationTokenSource: CancellationTokenSource;
// 	state: ServiceStatus;
// 	context?: T;
// }

// export enum ServiceStatus {
// 	NotRunning,
// 	StartingUp,
// 	Running,
// 	ShuttingDown,
// 	CrashingDown,
// 	Crashed
// }

// export class Service<T> {
// 	public constructor(name: string) {
// 		this.#name = name;
// 		this.#lastError = null;
// 		this.#instance = null;
// 	}

// 	#lastError: Error | null;
// 	#instance: ServiceContext<T> | null;

// 	readonly #name: string;

// 	public get name() {
// 		return this.#name;
// 	}


// 	protected get cancellationToken() {
// 		const token = this.#instance?.cancellationTokenSource.token;

// 		if (token == null) {
// 			throw new Error('Service is not running.');
// 		}

// 		return token;
// 	}

// 	protected get context() {
// 		if (this.#instance?.context == null) {
// 			throw new Error('Service is not running.');
// 		}

// 		return this.#instance;
// 	}
// }
