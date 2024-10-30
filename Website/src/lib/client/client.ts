/* eslint-disable @typescript-eslint/no-explicit-any */
import { Buffer } from 'buffer';
import { getContext, setContext } from 'svelte';
import { get, writable, type Readable } from 'svelte/store';

const clientContextName = 'Client Context';

const PACKET_REQUEST = 0 as const;
const PACKET_REQUEST_CANCEL = 1 as const;
const PACKET_RESPONSE = 2 as const;
const PACKET_RESPONSE_CANCEL = 3 as const;
const PACKET_RESPONSE_ERROR = 4 as const;
const PACKET_RESPONSE_INTERNAL_ERROR = 5 as const;

const INTERNAL_ERROR_DUPLICATE_ID = 0 as const;

export enum ClientStateType {
	Connecting,
	Connected,
	Failed
}

export type ClientState =
	| {
			state: ClientStateType.Connecting;
			request: (data: Buffer) => Promise<Buffer>;
	  }
	| {
			state: ClientStateType.Connected;
			request: (data: Buffer) => Promise<Buffer>;
	  }
	| {
			state: ClientStateType.Failed;
			error: Error;
			retry: () => void;
	  };

export function createClient(
	handleRequest: (data: Buffer, isCancelled: () => boolean) => Promise<Buffer>
): Readable<ClientState> {
	const preRequest: ((request: (data: Buffer) => Promise<Buffer>) => Promise<void>)[] = [];
	const state = writable<ClientState>({
		state: ClientStateType.Connecting,
		request: (data): Promise<Buffer> =>
			new Promise<Buffer>((resolve, reject) => {
				preRequest.push((request) => request(data).then(resolve, reject));
			})
	});

	const connect = () => {
		const url = `ws${window.location.protocol === 'https:' ? 's' : ''}://${window.location.hostname}:8082`;
		const responses: Map<
			string,
			{ resolve: (data: Buffer) => void; reject: (reason: Error) => void }
		> = new Map();
		const request: Map<string, () => void> = new Map();

		let nextId: number = 0;

		try {
			state.set({
				state: ClientStateType.Connecting,
				request: (data): Promise<Buffer> =>
					new Promise<Buffer>((resolve, reject) => {
						preRequest.push((request) => request(data).then(resolve, reject));
					})
			});

			const webSocket = new WebSocket(url);
			webSocket.binaryType = 'arraybuffer';

			webSocket.onmessage = ({ data: raw }: { data: ArrayBuffer }) => {
				const data = Buffer.from(new Uint8Array(raw));

				switch (data[0]) {
					case PACKET_REQUEST: {
						const id = data.subarray(1, 5);
						if (request.has(id.toString('hex'))) {
							webSocket.send(
								Buffer.concat([
									Buffer.from([PACKET_RESPONSE_INTERNAL_ERROR]),
									id,
									Buffer.from([INTERNAL_ERROR_DUPLICATE_ID])
								])
							);

							break;
						}

						let cancelled: boolean = false;

						request.set(id.toString('hex'), () => {
							cancelled = true;
						});

						void (async () => {
							try {
								let result: Buffer;
								try {
									result = await handleRequest(data.subarray(5), () => cancelled);
								} catch (error: any) {
									webSocket.send(
										Buffer.concat([
											Buffer.from([PACKET_RESPONSE_ERROR]),
											id,
											error instanceof ClientResponseError ? error.errorData : Buffer.alloc(0)
										])
									);
									return;
								}

								webSocket.send(Buffer.concat([Buffer.from([PACKET_RESPONSE]), id, result]));
							} finally {
								request.delete(id.toString('hex'));
							}
						})();

						break;
					}

					case PACKET_REQUEST_CANCEL: {
						const id = data.subarray(1, 5);
						const cancel = request.get(id.toString('hex'));

						if (cancel == null) {
							break;
						}

						cancel();
						break;
					}

					case PACKET_RESPONSE: {
						const id = data.subarray(1, 5);
						const response = responses.get(id.toString('hex'));

						if (response == null) {
							break;
						}

						const { resolve } = response;

						resolve(data.subarray(5));
						break;
					}

					case PACKET_RESPONSE_CANCEL: {
						const id = data.subarray(1, 5);
						const response = responses.get(id.toString('hex'));

						if (response == null) {
							break;
						}

						const { reject } = response;

						reject(new ClientResponseCancelledError());
						break;
					}

					case PACKET_RESPONSE_ERROR: {
						const id = data.subarray(1, 5);
						const response = responses.get(id.toString('hex'));

						if (response == null) {
							break;
						}

						const { reject } = response;

						reject(new ClientResponseError(data.subarray(5)));
						break;
					}

					case PACKET_RESPONSE_INTERNAL_ERROR: {
						const id = data.subarray(1, 5);
						const response = responses.get(id.toString('hex'));

						if (response == null) {
							break;
						}

						const { reject } = response;

						reject(new ClientInternalResponseError(data[5]));
						break;
					}
				}
			};

			webSocket.onopen = () => {
				const newState: ClientState & { state: ClientStateType.Connected } = {
					state: ClientStateType.Connected,
					request: async (data) => {
						const id = Buffer.alloc(4);
						id.writeInt32LE(nextId++, 0);

						try {
							return await new Promise<Buffer>((resolve, reject) => {
								responses.set(id.toString('hex'), { resolve, reject });
								webSocket.send(Buffer.concat([Buffer.from([PACKET_REQUEST]), id, data]));
							});
						} finally {
							responses.delete(id.toString('hex'));
						}
					}
				};

				state.set(newState);

				for (let index = 0; index < preRequest.length; index++) {
					void preRequest.splice(index--, 1)[0](newState.request);
				}
			};

			webSocket.onerror = webSocket.onclose = () => {
				if (get(state).state === ClientStateType.Failed) {
					return;
				}

				state.set({
					state: ClientStateType.Failed,
					error: new Error('Failed to connect'),
					retry: () => connect()
				});
			};
		} catch (error: any) {
			state.set({ state: ClientStateType.Failed, error, retry: () => connect() });
		}
	};

	connect();
	return state;
}

export class ClientError extends Error {
	public constructor(message?: string, options?: ErrorOptions) {
		super(message, options);
	}
}

export class ClientResponseError extends ClientError {
	public constructor(errorData: Buffer, options?: ErrorOptions) {
		super(`Remote returned an error`, options);

		this.#errorData = errorData;
	}

	readonly #errorData: Buffer;

	get errorData() {
		return this.#errorData;
	}
}

export class ClientResponseCancelledError extends ClientError {
	public constructor(options?: ErrorOptions) {
		super(`Remote cancelled the request`, options);
	}
}

export class ClientInternalResponseError extends Error {
	public constructor(reason: number, options?: ErrorOptions) {
		super(`Remote returned an internal error`, options);

		this.#reason = reason;
	}

	readonly #reason: number;

	get reason() {
		return this.#reason;
	}
}
export interface ClientContext {
	client: Readable<ClientState>;

	request: (data: Buffer) => Promise<Buffer>;
}

export function createClientContext(
	handleRequest: (data: Buffer, isCancelled: () => boolean) => Promise<Buffer>
) {
	const client = createClient(handleRequest);
	const context = setContext<ClientContext>(clientContextName, {
		client,
		request: async (data: Buffer): Promise<Buffer> => {
			const state = get(client);

			if (state.state === ClientStateType.Failed) {
				throw new Error('Not connected');
			}

			return await state.request(data);
		}
	});

	return {
		client,
		context
	};
}

export function getClientContext() {
	return getContext<ClientContext>(clientContextName);
}
