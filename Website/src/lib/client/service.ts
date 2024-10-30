import EventEmitter from '@rizzzi/eventemitter';
import { Logger, LogLevel } from './logger';
import type { EventInterface } from '@rizzzi/eventemitter';
import {
	CancellationToken,
	CancellationTokenSource,
	createCancellationToken,
	isEqual,
	OperationCancelledError,
	PromiseCompletionSource,
	waitAsync
} from './promise-source';

export enum ServiceState {
	NotRunning,
	StartingUp,
	Running,
	ShuttingDown,
	CrashingDown,
	Crashed
}

interface ServiceInstance<C> {
	promise: Promise<void>;
	cancellationTokenSource: CancellationTokenSource;
	state: ServiceState;
	context?: [C];
	promisesBeforeStopping: Promise<void>[];
}

interface ServiceEventInterface extends EventInterface {
	log: [level: LogLevel, scope: string[], message: string, timestamp: number];
	error: [error: Error];
	state: [state: ServiceState];
}

export abstract class Service<C> implements IService {
	static async startServices(
		services: IService[],
		cancellationToken: CancellationToken = createCancellationToken(false)
	) {
		const startedServices: IService[] = [];

		try {
			for (const service of [...services]) {
				await service.start(cancellationToken);

				startedServices.push(service);
			}

			// eslint-disable-next-line @typescript-eslint/no-explicit-any
		} catch (error: any) {
			const stopErrors: Error[] = [];

			for (const service of startedServices.toReversed()) {
				try {
					await service.stop();
					// eslint-disable-next-line @typescript-eslint/no-explicit-any
				} catch (error: any) {
					stopErrors.push(error);
				}
			}

			if (stopErrors.length == 0) {
				throw error;
			}

			throw new AggregateError([error, ...stopErrors]);
		}
	}

	static async stopServices(services: IService[]) {
		const stopErrors: Error[] = [];

		for (const service of services) {
			try {
				await service.stop();
				// eslint-disable-next-line @typescript-eslint/no-explicit-any
			} catch (error: any) {
				stopErrors.push(error);
			}
		}

		if (stopErrors.length == 0) {
			return;
		}

		throw new AggregateError([...stopErrors]);
	}

	constructor(name: string, downstream: IService | Logger | undefined = undefined) {
		this.#lastError = null;
		this.#internalContext = null;
		this.#logger = new Logger(name);
		this.#name = name;
		this.#events = new EventEmitter({ requireErrorHandling: false });

		if (downstream instanceof Service) {
			downstream.#logger.subscribe(this.#logger);
		} else if (downstream instanceof Logger) {
			downstream.subscribe(this.#logger);
		}

		this.#logger.on('log', async (level, scope, message, timestamp) => {
			await this.#events.emit('log', level, scope, message, timestamp);
		});
	}
	readonly #logger: Logger;
	readonly #name: string;
	readonly #events: EventEmitter<ServiceEventInterface, Service<C>>;

	#lastError: Error | null;
	#internalContext: ServiceInstance<C> | null;
	#ensureInternalContext() {
		const internalContext = this.#internalContext;

		if (internalContext == null) {
			throw new Error('Service is not running');
		}

		return internalContext;
	}

	public get name() {
		return this.#name;
	}

	public get state() {
		return this.#internalContext?.state ?? ServiceState.NotRunning;
	}

	public get logger() {
		return this.#logger;
	}

	public get on() {
		return this.#logger.on.bind(this.#logger);
	}

	public get once() {
		return this.#logger.once.bind(this.#logger);
	}

	public get off() {
		return this.#logger.off.bind(this.#logger);
	}

	protected get cancellationToken() {
		const serviceContext = this.#ensureInternalContext();

		if (serviceContext.context == null) {
			throw new Error('Context has not yet been initialized');
		}

		return serviceContext.cancellationTokenSource.token;
	}

	protected get promisesBeforeStopping() {
		return this.#ensureInternalContext().promisesBeforeStopping;
	}

	protected get context() {
		const serviceContext = this.#internalContext;

		if (serviceContext == null || serviceContext.context == null) {
			throw new Error('Context has not yet been initialized');
		}

		return serviceContext.context[0];
	}

	abstract onStart(
		startupCancellationToken: CancellationToken,
		cancellationToken: CancellationToken
	): Promise<C>;
	abstract onRun(context: C, cancellationToken: CancellationToken): Promise<void>;
	abstract onStop(context: C, error?: Error): Promise<void>;

	log(level: LogLevel, message: string, scope?: string) {
		this.#logger.log(level, `${scope != null ? `[${scope}] ` : ''}${message}`);
	}

	debug(message: string, scope?: string) {
		this.log(LogLevel.Debug, message, scope);
	}

	info(message: string, scope?: string) {
		this.log(LogLevel.Info, message, scope);
	}

	warn(message: string | Error, scope?: string) {
		this.log(LogLevel.Warn, message instanceof Error ? printable(message) : `${message}`, scope);
	}

	error(message: string | Error, scope?: string) {
		this.log(LogLevel.Error, message instanceof Error ? printable(message) : `${message}`, scope);
	}

	fatal(message: string | Error, scope?: string) {
		this.log(LogLevel.Fatal, message instanceof Error ? printable(message) : `${message}`, scope);
	}

	#setState(instance: ServiceInstance<C>, state: ServiceState) {
		this.info(
			`${ServiceState[instance.state]} -> ${ServiceState[(instance.state = state)]}`,
			'State'
		);
	}

	async #prepare(
		serviceContext: ServiceInstance<C>,
		startupCancellationToken: CancellationToken,
		startupTaskCompletionSource: PromiseCompletionSource,
		serviceCancellationTokenSource: CancellationTokenSource
	) {
		this.#setState(serviceContext, ServiceState.StartingUp);

		try {
			serviceContext.context = [
				await this.#startInternal(startupCancellationToken, serviceCancellationTokenSource.token)
			];

			startupTaskCompletionSource.resolve();
			// eslint-disable-next-line @typescript-eslint/no-explicit-any
		} catch (error: any) {
			this.#setState(serviceContext, ServiceState.CrashingDown);
			this.#setState(serviceContext, ServiceState.Crashed);

			startupTaskCompletionSource.reject(error);
			throw error;
		}

		this.#setState(serviceContext, ServiceState.Running);

		try {
			await this.#runInternal(serviceContext.context[0], serviceCancellationTokenSource);

			// eslint-disable-next-line @typescript-eslint/no-explicit-any
		} catch (error: any) {
			this.#setState(serviceContext, ServiceState.CrashingDown);

			await this.#stopInternal(serviceContext.context[0], error);

			this.#setState(serviceContext, ServiceState.Crashed);
			throw error;
		}

		this.#setState(serviceContext, ServiceState.ShuttingDown);

		try {
			await this.#stopInternal(serviceContext.context[0]);
			// eslint-disable-next-line @typescript-eslint/no-explicit-any
		} catch (error: any) {
			this.#setState(serviceContext, ServiceState.CrashingDown);
			this.#setState(serviceContext, ServiceState.Crashed);
			throw error;
		}

		this.#setState(serviceContext, ServiceState.NotRunning);
	}

	async #startInternal(
		startupCancellationToken: CancellationToken,
		serviceCancellationToken: CancellationToken
	) {
		const startupLinkedCancellationTokenSource: CancellationTokenSource =
			new CancellationTokenSource([startupCancellationToken, serviceCancellationToken]);

		try {
			return await this.onStart(
				startupLinkedCancellationTokenSource.token,
				serviceCancellationToken
			);
		} finally {
			startupLinkedCancellationTokenSource[Symbol.dispose]();
		}
	}

	async #runInternal(context: C, serviceCancellationTokenSource: CancellationTokenSource) {
		const serviceCancellationToken = serviceCancellationTokenSource.token;

		try {
			const errors: Error[] = [];

			try {
				try {
					await this.onRun(context, serviceCancellationToken);
				} finally {
					serviceCancellationTokenSource[Symbol.dispose]();
				}

				// eslint-disable-next-line @typescript-eslint/no-explicit-any
			} catch (error: any) {
				errors.push(error);
			}

			for (const promise of this.#ensureInternalContext().promisesBeforeStopping) {
				try {
					await promise;
				} catch (error: unknown) {
					if (
						error instanceof OperationCancelledError &&
						isEqual(error.cancellationToken, serviceCancellationToken)
					) {
						continue;
					}

					errors.push(error as never);
				}
			}

			if (errors.length === 1) {
				throw errors[0];
			} else if (errors.length > 1) {
				throw new AggregateError(errors);
			}

			// eslint-disable-next-line @typescript-eslint/no-explicit-any
		} catch (error: any) {
			if (
				error instanceof OperationCancelledError &&
				isEqual(error.cancellationToken, serviceCancellationToken)
			) {
				return;
			}

			throw error;
		}
	}

	async #stopInternal(context: C, error?: Error) {
		try {
			await this.onStop(context, error);

			// eslint-disable-next-line @typescript-eslint/no-explicit-any
		} catch (innerError: any) {
			if (error != null) {
				throw new AggregateError([innerError, error]);
			}

			throw innerError;
		}
	}

	async start(startupCancellationToken: CancellationToken = createCancellationToken(false)) {
		const serviceCancellationTokenSource = new CancellationTokenSource();
		const startupPromiseCompletionSource = new PromiseCompletionSource();

		if (this.#internalContext != null) {
			throw new Error('Service is already running.');
		}

		this.#lastError = null;
		const initiation = new PromiseCompletionSource<ServiceInstance<C>>();

		this.#internalContext = {
			promise: (async () => {
				try {
					try {
						await this.#prepare(
							await initiation,
							startupCancellationToken,
							startupPromiseCompletionSource,
							serviceCancellationTokenSource
						);

						// eslint-disable-next-line @typescript-eslint/no-explicit-any
					} catch (error: any) {
						this.#lastError = error;
						this.fatal(printable(error), 'Fatal Error');
						this.#events.emit('error', error);
					}
				} finally {
					this.#internalContext = null;
				}
			})(),
			cancellationTokenSource: serviceCancellationTokenSource,
			state: ServiceState.NotRunning,
			promisesBeforeStopping: []
		};

		initiation.resolve(this.#internalContext);

		await startupPromiseCompletionSource;
	}

	async watch(cancellationToken: CancellationToken = createCancellationToken(false)) {
		if (this.#lastError != null) {
			throw this.#lastError;
		}

		const task = this.#internalContext?.promise ?? Promise.resolve();

		try {
			await waitAsync(task, cancellationToken);
			// eslint-disable-next-line @typescript-eslint/no-explicit-any
		} catch (error: any) {
			if (
				error instanceof OperationCancelledError &&
				isEqual(error.cancellationToken, cancellationToken)
			) {
				return;
			}

			throw error;
		}
	}

	async stop() {
		let serviceInstanceContext: ServiceInstance<C> | null = null;

		serviceInstanceContext = this.#internalContext;

		if (serviceInstanceContext == null) {
			return;
		} else {
			const state = serviceInstanceContext.state;

			if (!(state == ServiceState.Running || state == ServiceState.StartingUp)) {
				return;
			}
		}

		serviceInstanceContext.cancellationTokenSource.cancel();
		await serviceInstanceContext.promise.catch(() => {});
	}
}

export interface IService {
	name: string;
	logger: Logger;

	start: (cancellationToken: CancellationToken) => Promise<void>;
	watch: (cancellationToken: CancellationToken) => Promise<void>;
	stop: () => Promise<void>;
}

function printable(error: Error): string {
	return `${error.message}${error.stack != null ? `\n${error.stack}` : ''}`;
}
