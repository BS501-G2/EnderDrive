// import { Buffer } from 'buffer';
// import {
// 	CancellationTokenSource,
// 	createCancellationToken,
// 	OperationCancelledError,
// 	PromiseCompletionSource,
// 	waitAll,
// 	waitAny,
// 	waitAsync,
// 	type CancellationToken
// } from './promise-source';
// import { Service } from './service';
// import { WaitQueue } from './wait-queue';
// import { getContext, setContext } from 'svelte';
// import { derived, type Readable, type Writable } from 'svelte/store';

// export class ClientOptions {
// 	name: string = 'Client';
// }

// export class ClientRequest {
// 	public constructor(
// 		source: PromiseCompletionSource<Buffer>,
// 		request: Buffer,
// 		cancellationToken: CancellationToken
// 	) {
// 		this.#source = source;
// 		this.#request = request;
// 		this.#cancellationToken = cancellationToken;
// 	}

// 	readonly #source: PromiseCompletionSource<Buffer>;
// 	readonly #request: Buffer;
// 	readonly #cancellationToken: CancellationToken;

// 	get request() {
// 		return this.#request;
// 	}

// 	get cancellationToken() {
// 		return this.#cancellationToken;
// 	}

// 	sendResponse(bytes: Buffer) {
// 		this.#source.resolve(bytes);
// 	}

// 	sendErrorResponse(bytes: Buffer) {
// 		this.#source.reject(new ClientResponseError(bytes));
// 	}

// 	sendCancelResponse() {
// 		this.#source.cancel(this.#cancellationToken);
// 	}
// }

// export interface InternalClientContext {
// 	requestCancellationTokenSources: Map<string, CancellationTokenSource>;
// 	responseTaskCompletionSources: Map<string, PromiseCompletionSource<Buffer>>;

// 	receiveDone: boolean;
// 	requests: WaitQueue<ClientRequest | null>;
// 	feed: WaitQueue<WorkerFeed>;

// 	webSocket: WebSocket;
// 	webSocketQueue: WaitQueue<Buffer | null>;

// 	nextRequestId: number;
// }

// type WorkerFeed =
// 	| [type: 0, bytes: Buffer]
// 	| [type: 1, bytes: Buffer]
// 	| [type: 2]
// 	| [type: 3]
// 	| [type: 4, error: Error];

// const PACKET_REQUEST = 0;
// const PACKET_REQUEST_CANCEL = 1;
// const PACKET_RESPONSE = 2;
// const PACKET_RESPONSE_CANCEL = 3;
// const PACKET_RESPONSE_ERROR = 4;
// const PACKET_RESPONSE_INTERNAL_ERROR = 5;

// const INTERNAL_ERROR_INVALID_ID = 0;

// export class Client extends Service<InternalClientContext> {
// 	public constructor(options: ClientOptions) {
// 		super(options.name);
// 	}

// 	async #receive(
// 		context: InternalClientContext,
// 		cancellationToken: CancellationToken
// 	): Promise<Buffer | null> {
// 		if (context.receiveDone) {
// 			return null;
// 		}
// 		const bytes = await context.webSocketQueue.dequeue(cancellationToken);

// 		if (bytes == null) {
// 			context.receiveDone = true;
// 		}
// 		return bytes;
// 	}

// 	async #send(context: InternalClientContext, bytes: Buffer) {
// 		context.webSocket.send(new Uint8Array(bytes));
// 	}

// 	async onStart(
// 		startupCancellationToken: CancellationToken,
// 		cancellationToken: CancellationToken
// 	): Promise<InternalClientContext> {
// 		const url = `ws${window.location.protocol === 'https:' ? 's' : ''}://${window.location.hostname}:8082`;
// 		const webSocket = new WebSocket(url);

// 		this.debug(url, 'WebSocket URL');

// 		const context: InternalClientContext = {
// 			requestCancellationTokenSources: new Map(),
// 			responseTaskCompletionSources: new Map(),

// 			receiveDone: false,
// 			requests: new WaitQueue(null),
// 			feed: new WaitQueue(null),

// 			webSocket,
// 			webSocketQueue: new WaitQueue(null),

// 			nextRequestId: 0
// 		};

// 		const promiseSource = new PromiseCompletionSource();

// 		webSocket.binaryType = 'arraybuffer';
// 		webSocket.onmessage = (message: MessageEvent) => {
// 			const { data }: { data: ArrayBuffer } = message;
// 			context.webSocketQueue.enqueue(Buffer.from(data));
// 		};
// 		webSocket.onopen = () => {
// 			promiseSource.resolve();
// 		};
// 		webSocket.onerror = () => {
// 			promiseSource.reject(new Error('Failed to connect'));
// 		};

// 		const unregister = startupCancellationToken.register(() => {
// 			webSocket.close();
// 		});

// 		await waitAsync(promiseSource.promise, cancellationToken);
// 		unregister();

// 		return context;
// 	}

// 	async onRun(context: InternalClientContext, cancellationToken: CancellationToken): Promise<void> {
// 		context.webSocket.onerror = () => {
// 			context.feed.enqueue([4, new Error('Connection closed.')]);
// 		};

// 		context.webSocket.onclose = () => {
// 			// context.feed.enqueue([2]);
// 			context.feed.enqueue([4, new Error('Connection closed.')]);
// 		};

// 		const promises: Promise<void>[] = [
// 			this.#runReceiveLoop(context, cancellationToken),
// 			this.#runWorker(context, cancellationToken)
// 		];

// 		await waitAny(promises, cancellationToken);
// 		this.promisesBeforeStopping.push(...promises);
// 	}

// 	async #runReceiveLoop(context: InternalClientContext, cancellationToken: CancellationToken) {
// 		while (true) {
// 			const buffer = await this.#receive(context, cancellationToken);

// 			if (buffer == null) {
// 				await context.feed.enqueue([2], cancellationToken);
// 				break;
// 			}

// 			await context.feed.enqueue([1, buffer], cancellationToken);
// 		}
// 	}

// 	async #runWorker(context: InternalClientContext, cancellationToken: CancellationToken) {
// 		const promises: Promise<void>[] = [];

// 		try {
// 			while (true) {
// 				const feed = await context.feed.dequeue(cancellationToken);
// 				if (feed == null) {
// 					break;
// 				}

// 				if (feed[0] === 0) {
// 					const [, bytes] = feed;
// 					await this.#send(context, bytes);
// 				} else if (feed[0] === 1) {
// 					const [, bytes] = feed;
// 					const promise = this.#handleReceivedPacket(context, bytes, cancellationToken);

// 					const monitor = async () => {
// 						promises.push(promise);

// 						try {
// 							await promise;
// 							// eslint-disable-next-line @typescript-eslint/no-explicit-any
// 						} catch (error: any) {
// 							await context.feed.enqueue([4, error]);
// 						} finally {
// 							const index = promises.indexOf(promise);
// 							if (index >= 0) {
// 								promises.splice(index, 1);
// 							}
// 						}
// 					};

// 					void monitor();
// 				} else if (feed[0] === 2) {
// 					context.receiveDone = true;
// 					await waitAll(promises, createCancellationToken(false));
// 				} else if (feed[0] === 3) {
// 					return;
// 				} else if (feed[0] === 4) {
// 					const [, error] = feed;
// 					throw error;
// 				}
// 			}
// 		} finally {
// 			await context.requests.enqueue(null);

// 			if (promises.length > 0) {
// 				await waitAll(promises, cancellationToken);
// 			}
// 		}
// 	}

// 	async #handleReceivedPacket(
// 		context: InternalClientContext,
// 		bytes: Buffer,
// 		cancellationToken: CancellationToken
// 	) {
// 		switch (bytes[0]) {
// 			case PACKET_REQUEST: {
// 				const cancellationTokenSource = new CancellationTokenSource([cancellationToken]);
// 				const id = bytes.subarray(1, 5).toString('hex');

// 				try {
// 					if (context.requestCancellationTokenSources.has(id)) {
// 						await context.feed.enqueue([
// 							0,
// 							Buffer.concat([
// 								Buffer.from([PACKET_RESPONSE_INTERNAL_ERROR]),
// 								Buffer.from(id, 'hex'),
// 								Buffer.from([INTERNAL_ERROR_INVALID_ID])
// 							])
// 						]);
// 					}

// 					let result: Buffer;

// 					try {
// 						const promiseCompletionSource = new PromiseCompletionSource<Buffer>();
// 						const request: ClientRequest = new ClientRequest(
// 							promiseCompletionSource,
// 							bytes.subarray(5),
// 							cancellationToken
// 						);

// 						await context.requests.enqueue(request, cancellationToken);
// 						result = await promiseCompletionSource;

// 						// eslint-disable-next-line @typescript-eslint/no-explicit-any
// 					} catch (error: any) {
// 						if (error instanceof OperationCancelledError) {
// 							await context.feed.enqueue([
// 								0,
// 								Buffer.concat([Buffer.from([PACKET_RESPONSE_CANCEL]), Buffer.from(id, 'hex')])
// 							]);
// 						} else {
// 							const buffer = Buffer.concat([
// 								error instanceof ClientResponseError ? error.errorData : Buffer.from(error.message)
// 							]);

// 							await context.feed.enqueue([
// 								0,
// 								Buffer.concat([
// 									Buffer.from([PACKET_RESPONSE_ERROR]),
// 									Buffer.from(id, 'hex'),
// 									buffer
// 								])
// 							]);
// 						}

// 						break;
// 					}

// 					await context.feed.enqueue([
// 						0,
// 						Buffer.concat([Buffer.from([PACKET_RESPONSE]), Buffer.from(id, 'hex'), result])
// 					]);

// 					context.requestCancellationTokenSources.delete(id);
// 				} finally {
// 					cancellationTokenSource[Symbol.dispose]();
// 				}

// 				break;
// 			}

// 			case PACKET_REQUEST_CANCEL: {
// 				const id = bytes.subarray(1, 5).toString('hex');

// 				const cancellationTokenSource = context.requestCancellationTokenSources.get(id);
// 				if (cancellationTokenSource == null) {
// 					break;
// 				}
// 				context.requestCancellationTokenSources.delete(id);

// 				cancellationTokenSource.cancel();
// 				break;
// 			}

// 			case PACKET_RESPONSE: {
// 				const id = bytes.subarray(1, 5).toString('hex');

// 				const promiseCompletionSource = context.responseTaskCompletionSources.get(id);
// 				if (promiseCompletionSource == null) {
// 					break;
// 				}
// 				context.responseTaskCompletionSources.delete(id);

// 				promiseCompletionSource.resolve(bytes.subarray(5));
// 				break;
// 			}

// 			case PACKET_RESPONSE_CANCEL: {
// 				const id = bytes.subarray(1, 5).toString('hex');

// 				const promiseCompletionSource = context.responseTaskCompletionSources.get(id);
// 				if (promiseCompletionSource == null) {
// 					break;
// 				}
// 				context.responseTaskCompletionSources.delete(id);

// 				promiseCompletionSource.cancel(createCancellationToken(true));
// 				break;
// 			}

// 			case PACKET_RESPONSE_ERROR: {
// 				const id = bytes.subarray(1, 5).toString('hex');

// 				const promiseCompletionSource = context.responseTaskCompletionSources.get(id);
// 				if (promiseCompletionSource == null) {
// 					break;
// 				}
// 				context.responseTaskCompletionSources.delete(id);

// 				promiseCompletionSource.reject(new ClientResponseError(bytes.subarray(5)));
// 				break;
// 			}

// 			case PACKET_RESPONSE_INTERNAL_ERROR: {
// 				const id = bytes.subarray(1, 5).toString('hex');

// 				const promiseCompletionSource = context.responseTaskCompletionSources.get(id);
// 				if (promiseCompletionSource == null) {
// 					break;
// 				}
// 				context.responseTaskCompletionSources.delete(id);

// 				promiseCompletionSource.reject(new ClientInternalResponseError(bytes[9]));
// 				break;
// 			}
// 		}
// 	}

// 	// eslint-disable-next-line @typescript-eslint/no-unused-vars
// 	async onStop(context: InternalClientContext, error?: Error): Promise<void> {
// 		context.webSocket.close();
// 	}

// 	#getNextId(): number {
// 		const maxInt32 = 2147483647;

// 		let result = ++this.context.nextRequestId;

// 		if (result >= maxInt32) {
// 			return (this.context.nextRequestId = result = 0);
// 		}

// 		return result;
// 	}

// 	async sendRequest(
// 		bytes: Buffer,
// 		cancellationToken: CancellationToken = createCancellationToken(false)
// 	): Promise<Buffer> {
// 		while (true) {
// 			const source = new PromiseCompletionSource<Buffer>();
// 			const id = (() => {
// 				const id = Buffer.alloc(4);
// 				id.writeUInt32LE(this.#getNextId(), 0);
// 				return id.toString('hex');
// 			})();

// 			if (this.context.responseTaskCompletionSources.has(id)) {
// 				continue;
// 			}

// 			this.context.responseTaskCompletionSources.set(id, source);

// 			await this.context.feed.enqueue(
// 				[0, Buffer.concat([Buffer.from([PACKET_REQUEST]), Buffer.from(id, 'hex'), bytes])],
// 				cancellationToken
// 			);

// 			void (async () => {
// 				await cancellationToken.getPromise();

// 				try {
// 					await this.context.feed.enqueue([
// 						0,
// 						Buffer.concat([Buffer.from([PACKET_REQUEST_CANCEL]), Buffer.from(id, 'hex')])
// 					]);

// 					// eslint-disable-next-line @typescript-eslint/no-explicit-any
// 				} catch (error: any) {
// 					source.reject(error);
// 				}
// 			})();

// 			try {
// 				return await source.promise;
// 				// eslint-disable-next-line @typescript-eslint/no-explicit-any
// 			} catch (error: any) {
// 				if (error instanceof ClientInternalResponseError) {
// 					if (error.reason === INTERNAL_ERROR_INVALID_ID) {
// 						continue;
// 					}
// 				}

// 				throw error;
// 			}
// 		}
// 	}

// 	async receiveRequest(cancellationToken: CancellationToken = createCancellationToken(false)) {
// 		if (this.context.receiveDone) {
// 			return null;
// 		}

// 		const source = new CancellationTokenSource([cancellationToken, this.cancellationToken]);
// 		try {
// 			await this.context.requests.dequeue(source.token);
// 		} finally {
// 			source[Symbol.dispose]();
// 		}
// 	}
// }

// export class ClientError extends Error {
// 	public constructor(message?: string, options?: ErrorOptions) {
// 		super(message, options);
// 	}
// }

// export class ClientResponseError extends ClientError {
// 	public constructor(errorData: Buffer, options?: ErrorOptions) {
// 		super(`Remote returned an error`, options);

// 		this.#errorData = errorData;
// 	}

// 	readonly #errorData: Buffer;

// 	get errorData() {
// 		return this.#errorData;
// 	}
// }

// export class ClientInternalResponseError extends Error {
// 	public constructor(reason: number, options?: ErrorOptions) {
// 		super(`Remote returned an internal error`, options);

// 		this.#reason = reason;
// 	}

// 	readonly #reason: number;

// 	get reason() {
// 		return this.#reason;
// 	}
// }

// const clientContextName = 'Client Context';

// export type ClientState =
// 	| [state: 'inactive']
// 	| [state: 'starting']
// 	| [state: 'ready', request: Client]
// 	| [state: 'error', error: Error]
// 	| [state: 'cancelled'];

// export interface ClientContext {
// 	state: Readable<ClientState>;
// }

// export function createClientContext(state: Writable<ClientState>) {
// 	const context: ClientContext = setContext<ClientContext>(clientContextName, {
// 		state: derived(state, (value) => value)
// 	});

// 	return { state, context };
// }

// export function getClientContext() {
// 	return getContext<ClientContext>(clientContextName);
// }
