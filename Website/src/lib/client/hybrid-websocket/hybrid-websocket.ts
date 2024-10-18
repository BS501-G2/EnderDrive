import { get, writable, type Writable } from 'svelte/store';
import { HybridWebSocketStream } from './hybrid-websocket-stream';
import type { PromiseSource } from '$lib/client/promise-source';
import * as SocketIO from 'socket.io-client';
import { WaitQueue } from '../wait-queue';
import {
	HybridWebSocketPacketType,
	ShutdownChannelAbruptReason,
	type IHybridWebSocketPacket,
	type InitRequestPacket,
	type PacketWrap,
	type RequestBeginPacket,
	type RequestNextPacket,
	type ShutdownAbruptPacket
} from './hybrid-websocket-packet';

export interface HybridWebSocketContext {
	incomingRequests: Record<number, HybridWebSocketStream>;
	incomingResponses: Record<number, HybridWebSocketStream>;
	incomingResponseErrors: Record<number, HybridWebSocketStream>;
	incomingMessages: Record<number, HybridWebSocketStream>;
	incomingPongs: Record<number, PromiseSource<void>>;
	incomingShutdownCompletes: PromiseSource<void> | null;

	isShuttingDown: boolean;
	results: WaitQueue<HybridWebSocketResult>;
	receive: WaitQueue<PacketWrap<IHybridWebSocketPacket>>;
}

export class HybridWebSocket {
	public constructor() {
		const webSocket = SocketIO.connect('http://10.1.0.1:8083');

		webSocket.on('connect', () => {
			void this.#init();
		});

		webSocket.on('disconnect', () => {});

		webSocket.on('message', ({ type, packet }) => {
			console.log(type, packet);

			this.#context.receive.enqueue({ type, packet });
		});

		this.#webSocket = webSocket;
		this.#getContext = null;
	}

	readonly #webSocket: SocketIO.Socket;
	#getContext: (() => HybridWebSocketContext) | null;
	get #context() {
		const getContext = this.#getContext;

		if (getContext == null) {
			throw new Error('Not yet initialized');
		}

		return getContext();
	}

	async #send<T extends IHybridWebSocketPacket>(
		type: HybridWebSocketPacketType,
		packet: T
	): Promise<void> {
		const send = async (type: HybridWebSocketPacketType, packet: IHybridWebSocketPacket) => {
			this.#webSocket.emit('message', { type, packet });
		};

		try {
			await send(type, packet);
		} catch (error: unknown) {
			if (this.#webSocket.connected) {
				try {
					await send(HybridWebSocketPacketType.ShutdownAbrupt, {
						Reason: ShutdownChannelAbruptReason.InternalError
					} as ShutdownAbruptPacket);
				} catch (error2: unknown) {
					throw new AggregateError(error as never, error2 as never);
				}
			}

			throw error;
		}
	}

	async #receive(
		expect?: () => (HybridWebSocketPacketType | null)[]
	): Promise<PacketWrap<IHybridWebSocketPacket>> {
		const receive: () => Promise<PacketWrap<IHybridWebSocketPacket>> = async () => {
			const { type, packet } = await this.#context.receive.dequeue();

			if (expect != null && !expect().includes(type)) {
				throw new UnexpectedPacketError(type, packet);
			}

			return { type, packet };
		};

		try {
			return await receive();
		} catch (error: unknown) {
			try {
				if (this.#webSocket.connected) {
					await this.#send(HybridWebSocketPacketType.ShutdownAbrupt, {
						Reason:
							error instanceof UnexpectedPacketError
								? ShutdownChannelAbruptReason.UnexpectedPacket
								: ShutdownChannelAbruptReason.InternalError
					} as ShutdownAbruptPacket);
				}
			} catch (error2: unknown) {
				throw new AggregateError(error as never, error2 as never);
			}

			throw error;
		}
	}

	async #onStart(): Promise<HybridWebSocketContext> {
		while (true) {
			await this.#send<InitRequestPacket>(HybridWebSocketPacketType.InitRequest, {});

			const { type, packet } = await this.#receive(() => [
				HybridWebSocketPacketType.InitResponse,
				HybridWebSocketPacketType.ShutdownAbrupt
			]);

			if (type === HybridWebSocketPacketType.InitResponse) {
				const context: HybridWebSocketContext = {
					incomingRequests: {},
					incomingResponses: {},
					incomingResponseErrors: {},
					incomingMessages: {},
					incomingPongs: {},
					incomingShutdownCompletes: null,
					receive: new WaitQueue(null),
					isShuttingDown: false,
					results: new WaitQueue(null)
				};

				return context;
			} else if (type === HybridWebSocketPacketType.ShutdownAbrupt) {
				const shutdownAbruptPacket = packet as ShutdownAbruptPacket;

				throw new AbruptShutdownError(shutdownAbruptPacket.Reason);
			}
		}
	}

	async #onRun(context: HybridWebSocketContext) {
		while (true) {
			const { type, packet } = await this.#receive(() => [
				HybridWebSocketPacketType.RequestBegin,
				HybridWebSocketPacketType.RequestNext,
				HybridWebSocketPacketType.RequestDone,
				HybridWebSocketPacketType.RequestAbort,
				HybridWebSocketPacketType.MessageBegin,
				HybridWebSocketPacketType.MessageNext,
				HybridWebSocketPacketType.MessageDone,
				HybridWebSocketPacketType.MessageAbort,
				HybridWebSocketPacketType.ResponseBegin,
				HybridWebSocketPacketType.ResponseNext,
				HybridWebSocketPacketType.ResponseDone,
				HybridWebSocketPacketType.ResponseErrorBegin,
				HybridWebSocketPacketType.ResponseErrorNext,
				HybridWebSocketPacketType.ResponseErrorDone,
				HybridWebSocketPacketType.Ping,
				HybridWebSocketPacketType.Pong,
				HybridWebSocketPacketType.ShutdownAbrupt,
				HybridWebSocketPacketType.Shutdown,
				context.isShuttingDown ? HybridWebSocketPacketType.ShutdownComplete : null
			]);

			switch (type) {
				case HybridWebSocketPacketType.RequestBegin: {
					const requestBeginPacket = packet as RequestBeginPacket;

					const requestStream = new HybridWebSocketStream();
					const responseStream = new HybridWebSocketStream();

					context.incomingRequests[requestBeginPacket.RequestId] = requestStream;

					const request = new HybridWebSocketRequestResult(requestStream, responseStream);

					await context.results.enqueue(request);
					await requestStream.push(requestBeginPacket.RequestData);

					break;
				}

				case HybridWebSocketPacketType.RequestNext: {
					const requestNextPacket = packet as RequestNextPacket;

					await context.incomingRequests[requestNextPacket.RequestId].push(
						requestNextPacket.RequestData
					);
					break;
				}
			}
		}
	}

	async #onStop(context: HybridWebSocketContext, error?: Error) {
		console.error(error);
	}

	async #init() {
		while (true) {
			try {
				const context = await this.#onStart();
				this.#getContext = () => context;

				try {
					await this.#onRun(context);
				} catch (error: unknown) {
					await this.#onStop(context, error as never);
					continue;
				}

				await this.#onStop(context);
			} finally {
				this.#getContext = null;
			}
		}
	}
}

const client: Writable<HybridWebSocket | null> = writable(null);

export function getClient() {
	let currentClient: HybridWebSocket | null = get(client);

	if (currentClient == null) {
		currentClient = new HybridWebSocket();
		client.set(currentClient);
	}

	return currentClient;
}

export class UnexpectedPacketError extends Error {
	public constructor(type: HybridWebSocketPacketType, packet: IHybridWebSocketPacket) {
		super(`Unexpected packet: ${type}`);

		this.#packet = packet;
	}

	readonly #packet: IHybridWebSocketPacket;
	public get packet() {
		return this.#packet;
	}
}

export class AbruptShutdownError extends Error {
	public constructor(reason: ShutdownChannelAbruptReason) {
		super();

		this.#reason = reason;
	}

	readonly #reason: ShutdownChannelAbruptReason;
	public get reason() {
		return this.#reason;
	}
}

export abstract class HybridWebSocketResult {}

export class HybridWebSocketRequestResult extends HybridWebSocketResult {
	public constructor(request: HybridWebSocketStream, response: HybridWebSocketStream) {
		super();

		this.#request = request;
		this.#response = response;
	}

	readonly #request: HybridWebSocketStream;
	readonly #response: HybridWebSocketStream;

	public get request() {
		return this.#request;
	}

	public get response() {
		return this.#response;
	}
}

export class HybridWebSocketResponseResult extends HybridWebSocketResult {
	public constructor(messageData: HybridWebSocketStream) {
		super();

		this.#messageData = messageData;
	}

	readonly #messageData: HybridWebSocketStream;

	public get messageData() {
		return this.#messageData;
	}
}
