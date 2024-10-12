export const CLIENT_PACKET_TYPE_INIT_REQUEST = 0;
export const CLIENT_PACKET_TYPE_INIT_RESPONSE = 1;
export const CLIENT_PACKET_TYPE_REQUEST_BEGIN = 2;
export const CLIENT_PACKET_TYPE_REQUEST_NEXT = 3;
export const CLIENT_PACKET_TYPE_REQUEST_DONE = 4;
export const CLIENT_PACKET_TYPE_REQUEST_ABORT = 5;
export const CLIENT_PACKET_TYPE_MESSAGE_BEGIN = 6;
export const CLIENT_PACKET_TYPE_MESSAGE_NEXT = 7;
export const CLIENT_PACKET_TYPE_MESSAGE_DONE = 8;
export const CLIENT_PACKET_TYPE_MESSAGE_ABORT = 9;
export const CLIENT_PACKET_TYPE_RESPONSE_BEGIN = 10;
export const CLIENT_PACKET_TYPE_RESPONSE_NEXT = 11;
export const CLIENT_PACKET_TYPE_RESPONSE_DONE = 12;
export const CLIENT_PACKET_TYPE_RESPONSE_ERROR_BEGIN = 13;
export const CLIENT_PACKET_TYPE_RESPONSE_ERROR_NEXT = 14;
export const CLIENT_PACKET_TYPE_RESPONSE_ERROR_DONE = 15;
export const CLIENT_PACKET_TYPE_PING = 16;
export const CLIENT_PACKET_TYPE_PONG = 17;
export const CLIENT_PACKET_TYPE_SHUTDOWN = 18;
export const CLIENT_PACKET_TYPE_SHUTDOWN_ABRUPT=19;
export const CLIENT_PACKET_TYPE_SHUTDOWN_COMPLETE=20;

export type ClientPacketType = typeof CLIENT_PACKET_TYPE_INIT_REQUEST;

export class ClientPacket<T extends ClientPacketType, P extends object = object> {
	public static parse<T extends ClientPacketType, P extends object = object>(data: {
		type: T;
		packet: P;
	}): ClientPacket<T, P> {
		return new ClientPacket(data.type, data.packet);
	}

	public constructor(type: T, packet: unknown) {
		this.#type = type;
		this.packet = packet as never;
	}

	readonly #type: T;
	protected readonly packet: P;

	public get type() {
		return this.#type;
	}
}

export class ClientInitPacket extends ClientPacket<typeof CLIENT_PACKET_TYPE_INIT_REQUEST, object> {
	public constructor() {
		super(CLIENT_PACKET_TYPE_INIT_REQUEST, {});
	}
}

export class ClientInitResponsePacket extends ClientPacket {}
