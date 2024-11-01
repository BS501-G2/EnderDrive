/* eslint-disable @typescript-eslint/no-explicit-any */
import { Buffer } from 'buffer';
import { getContext, setContext, type Snippet } from 'svelte';
import * as MsgPack from '@msgpack/msgpack';
import { derived, get, writable, type Readable } from 'svelte/store';
import * as SocketIO from 'socket.io-client';

const clientContextName = 'Client Context';

const PACKET_REQUEST = 0 as const;
const PACKET_REQUEST_CANCEL = 1 as const;
const PACKET_RESPONSE = 2 as const;
const PACKET_RESPONSE_CANCEL = 3 as const;
const PACKET_RESPONSE_ERROR = 4 as const;
const PACKET_RESPONSE_INTERNAL_ERROR = 5 as const;

const INTERNAL_ERROR_DUPLICATE_ID = 0 as const;

export type Packet =
	| { type: typeof PACKET_REQUEST; id: number; data: any }
	| {
			type: typeof PACKET_REQUEST_CANCEL;
			id: number;
	  }
	| {
			type: typeof PACKET_RESPONSE;
			id: number;
			data: any;
	  }
	| { type: typeof PACKET_RESPONSE_CANCEL; id: number }
	| {
			type: typeof PACKET_RESPONSE_ERROR;
			id: number;
			error: string;
	  }
	| {
			type: typeof PACKET_RESPONSE_INTERNAL_ERROR;
			id: number;
			reason: number;
	  };

export enum ClientStateType {
	Connecting,
	Connected,
	Failed
}

export type ClientState =
	| {
			state: ClientStateType.Connecting;
			request: (packet: ClientPacket<ServerSideRequestCode>) => Promise<ClientPacket<ResponseCode>>;
	  }
	| {
			state: ClientStateType.Connected;
			request: (packet: ClientPacket<ServerSideRequestCode>) => Promise<ClientPacket<ResponseCode>>;
	  }
	| {
			state: ClientStateType.Failed;
			error: Error;
			retry: () => void;
	  };

export interface ClientPacket<
	T extends ServerSideRequestCode | ClientSideRequestCode | ResponseCode
> {
	Code: T;
	Data: any;
}

export function createClient(
	handleRequest: (data: any, isCancelled: () => boolean) => Promise<any>
): Readable<ClientState> {
	const preRequest: {
		packet: ClientPacket<ServerSideRequestCode>;
		resolve: (data: ClientPacket<ResponseCode>) => void;
		reject: (error: Error) => void;
	}[] = [];

	const state = writable<ClientState>({
		state: ClientStateType.Connecting,
		request: (packet) =>
			new Promise<any>((resolve, reject) => preRequest.push({ packet, reject, resolve }))
	});

	const connect = async () => {
		const url = `ws${window.location.protocol === 'https:' ? 's' : ''}://${window.location.hostname}:8083`;
		const responses: Map<
			number,
			{ resolve: (data: any) => void; reject: (reason: Error) => void }
		> = new Map();
		const requests: Map<number, () => void> = new Map();

		let nextId: number = 0;

		try {
			if (get(state).state !== ClientStateType.Connecting) {
				state.set({
					state: ClientStateType.Connecting,
					request: (packet) =>
						new Promise<any>((resolve, reject) => preRequest.push({ packet, reject, resolve }))
				});
			}

			const socket = SocketIO.connect(url, {
				reconnection: false,
				autoConnect: false
			});

			function send(packet: Packet) {
				console.log('->', packet);
				socket.emit('message', packet);
			}

			socket.on('message', (packet: Packet) => {
				console.log('<-', packet);
				switch (packet.type) {
					case PACKET_REQUEST: {
						if (requests.has(packet.id)) {
							send({
								type: PACKET_RESPONSE_INTERNAL_ERROR,
								id: packet.id,
								reason: INTERNAL_ERROR_DUPLICATE_ID
							});
							break;
						}

						let cancelled: boolean = false;
						requests.set(packet.id, () => {
							cancelled = true;
						});

						void (async () => {
							try {
								let result: any;

								try {
									result = await handleRequest(packet.data, () => cancelled);
								} catch (error: any) {
									send({
										type: PACKET_RESPONSE_ERROR,
										id: packet.id,
										error: error instanceof Error ? error.message : ''
									});
									return;
								}

								send({
									type: PACKET_RESPONSE,
									id: packet.id,
									data: result
								});
							} finally {
								requests.delete(packet.id);
							}
						})();

						break;
					}

					case PACKET_REQUEST_CANCEL: {
						const cancel = requests.get(packet.id);
						if (cancel == null) {
							break;
						}

						cancel();
						break;
					}

					case PACKET_RESPONSE: {
						const response = responses.get(packet.id);
						if (response == null) {
							break;
						}

						const { resolve } = response;

						resolve(packet.data);
						break;
					}

					case PACKET_RESPONSE_CANCEL: {
						const response = responses.get(packet.id);
						if (response == null) {
							break;
						}

						const { reject } = response;

						reject(new ClientResponseCancelledError());
						break;
					}

					case PACKET_RESPONSE_ERROR: {
						const response = responses.get(packet.id);
						if (response == null) {
							break;
						}

						const { reject } = response;

						reject(new ClientResponseError(packet.error));
						break;
					}

					case PACKET_RESPONSE_INTERNAL_ERROR: {
						const response = responses.get(packet.id);
						if (response == null) {
							break;
						}

						const { reject } = response;

						reject(new ClientInternalResponseError(packet.reason));
						break;
					}
				}
			});

			socket.io.on('open', () => {
				const newState: ClientState & { state: ClientStateType.Connected } = {
					state: ClientStateType.Connected,
					request: async (packet) => {
						const id = nextId++;

						return new Promise<any>((resolve, reject) => {
							responses.set(id, { resolve, reject });

							send({
								type: PACKET_REQUEST,
								id,
								data: packet
							});
						});
					}
				};

				for (let index = 0; index < preRequest.length; index++) {
					const {
						[index--]: { packet, resolve, reject }
					} = preRequest;
					preRequest.splice(index, 1);

					newState.request(packet).then(resolve, reject);
				}

				state.set(newState);
			});

			function onClose(
				error: Error = new Error('Failed to establish a connection to the server.')
			) {
				if (get(state).state === ClientStateType.Failed) {
					return;
				}

				state.set({
					state: ClientStateType.Failed,
					error,
					retry: () => connect()
				});

				for (let index = 0; index < preRequest.length; index++) {
					const {
						[index--]: { reject }
					} = preRequest;
					preRequest.splice(index, 1);

					reject(new Error(`Connection Closed`, { cause: error }));
				}
			}

			socket.io.on('close', (reason) => {
				onClose(new Error(reason));
			});

			socket.io.on('error', (error: Error) => {
				onClose(error);
			});

			socket.connect();
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
	public constructor(errorData: string = 'Remote sent an error', options?: ErrorOptions) {
		super(errorData, options);

		this.#errorData = errorData;
	}

	readonly #errorData: string;

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

	setMainContent: Readable<Snippet | null>;

	request: (packet: ClientPacket<ServerSideRequestCode>) => Promise<ClientPacket<ResponseCode>>;

	setRequestHandler: (
		code: ClientSideRequestCode,
		handler: (data: Buffer, isCancelled: () => boolean) => Promise<Payload<ResponseCode>>
	) => () => void;

	functions: ServerSideRequests;
}

export interface Payload<T extends ClientSideRequestCode | ServerSideRequestCode | ResponseCode> {
	Code: T;
	Data: Buffer;
}

export function createClientContext() {
	const requestHandlers: Map<
		number,
		(data: Buffer, isCancelled: () => boolean) => Promise<Payload<ResponseCode>>
	> = new Map();

	const handleRequest = async (data: Buffer, isCancelled: () => boolean): Promise<Buffer> => {
		const requestPayload = MsgPack.decode(data) as Payload<ClientSideRequestCode>;

		const handler = requestHandlers.get(requestPayload.Code);

		const response: Payload<ResponseCode> =
			handler == null
				? {
						Code: ResponseCode.NoHandlerFound,
						Data: Buffer.alloc(0)
					}
				: await handler(requestPayload.Data, isCancelled);

		return Buffer.from(MsgPack.encode(response));
	};

	const client = createClient(handleRequest);
	const content = writable<Snippet | null>();
	const context = setContext<ClientContext>(clientContextName, {
		client,

		setMainContent: derived(content, (value) => value),

		request: async (
			packet: ClientPacket<ServerSideRequestCode>
		): Promise<ClientPacket<ResponseCode>> => {
			const state = get(client);

			if (state.state === ClientStateType.Failed) {
				throw new Error('Not connected');
			}

			return await state.request(packet);
		},

		setRequestHandler: (code, handler) => {
			requestHandlers.set(code, handler);

			return () => {
				const currentHandler = requestHandlers.get(code);

				if (handler != currentHandler) {
					return;
				}

				requestHandlers.delete(code);
			};
		},

		functions: getServerSideFunctions((): ClientContext['request'] => context.request)
	});

	getBuiltinRequestHandlers(context);

	return {
		client,
		content,
		context
	};
}

export function useClientContext() {
	return getContext<ClientContext>(clientContextName);
}

export enum ClientSideRequestCode {
	Ping
}

export enum ResponseCode {
	OK,
	Cancelled,
	InvalidParameters,
	NoHandlerFound,
	InvalidRequestCode,
	InternalError
}

function getBuiltinRequestHandlers(context: ClientContext) {
	context.setRequestHandler(ClientSideRequestCode.Ping, async () => {
		const code = ResponseCode.OK;
		const data = Buffer.alloc(0);

		return { Code: code, Data: data };
	});
}

export enum ServerSideRequestCode {
	Echo,

	WhoAmI,
	AuthenticatePassword,
	AuthenticateGoogle,

	CreateAdmin,
	SetupRequirements
}

function getServerSideFunctions(
	getRequestFunc: () => ClientContext['request']
): ServerSideRequests {
	async function request(code: ServerSideRequestCode, data: any): Promise<any> {
		const response = await getRequestFunc()({ Code: code, Data: data });

		if (response.Code !== ResponseCode.OK) {
			throw new Error('Failed');
		}

		return response.Data;
	}

	const functions: ServerSideRequests = {
		getSetupRequirements: (data) => request(ServerSideRequestCode.SetupRequirements, data),

		createAdmin: (data) => request(ServerSideRequestCode.CreateAdmin, data)
	};

	return functions;
}

export interface ServerSideRequests {
	getSetupRequirements: (
		data: object
	) => Promise<{ AdminSetupRequired: boolean }>;

	createAdmin: (data: {
		Username: string;
		Password: string;
		ConfirmPassword: string;
		LastName: string;
		FirstName: string;
		MiddleName: string | null;
		DisplayName: string | null;
	}) => Promise<object>;
}
