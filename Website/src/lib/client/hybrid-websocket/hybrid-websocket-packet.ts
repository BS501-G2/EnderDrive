/* eslint-disable @typescript-eslint/no-empty-object-type */
export enum HybridWebSocketPacketType {
	InitRequest,
	InitResponse,

	RequestBegin,
	RequestNext,
	RequestDone,
	RequestAbort,

	MessageBegin,
	MessageNext,
	MessageDone,
	MessageAbort,

	ResponseBegin,
	ResponseNext,
	ResponseDone,

	ResponseErrorBegin,
	ResponseErrorNext,
	ResponseErrorDone,

	Ping,
	Pong,

	Shutdown,
	ShutdownAbrupt,
	ShutdownComplete
}

export interface IHybridWebSocketPacket {}

export interface IRequestPacket extends IHybridWebSocketPacket {
	RequestId: number;
}

export interface IMessagePacket extends IHybridWebSocketPacket {
	MessageId: number;
}

export interface IResponsePacket extends IHybridWebSocketPacket {
	RequestId: number;
}

export interface IResponseFeedPacket extends IResponsePacket {
	ResponseData: Uint8Array;
}

export interface IResponseErrorPacket extends IHybridWebSocketPacket {
	RequestId: number;
}

export interface InitRequestPacket extends IHybridWebSocketPacket {}

export interface InitResponsePacket extends IHybridWebSocketPacket {}

export interface RequestBeginPacket extends IRequestPacket {
	RequestData: Uint8Array;
}

export interface RequestNextPacket extends IRequestPacket {
	RequestData: Uint8Array;
}

export interface RequestDonePacket extends IRequestPacket {}

export interface RequestAbortPacket extends IRequestPacket {}

export interface MessageBeginPacket extends IMessagePacket {
	MessageData: Uint8Array;
}

export interface MessageNextPacket extends IMessagePacket {
	MessageData: Uint8Array;
}

export interface MessageDonePacket extends IMessagePacket {}

export interface MessageAbortPacket extends IMessagePacket {}

export interface ResponseBeginPacket extends IResponseFeedPacket {}

export interface ResponseNextPacket extends IResponseFeedPacket {}

export interface ResponseDonePacket extends IResponsePacket {}

export interface ResponseErrorBeginPacket extends IResponseErrorPacket {
	RequestData: Uint8Array;
}

export interface ResponseErrorNextPacket extends IResponseErrorPacket {
	RequestData: Uint8Array;
}

export interface ResponseErrorDonePacket extends IResponseErrorPacket {}

export interface PingPacket extends IHybridWebSocketPacket {
	PingId: number;
}

export interface PongPacket extends IHybridWebSocketPacket {
	PingId: number;
}

export interface ShutdownPacket extends IHybridWebSocketPacket {}

export enum ShutdownChannelAbruptReason {
	UnexpectedPacket,
	InternalError,
	InvalidRequestId,
	InvalidMessageId,
	InvalidPingId,
	DuplicateRequestId,
	DuplicateMessageId,
	DuplicatePingId
}

export interface ShutdownAbruptPacket extends IHybridWebSocketPacket {
	Reason: ShutdownChannelAbruptReason;
}

export interface ShutdownCompletePacket extends IHybridWebSocketPacket {}

export interface PacketWrap<T extends IHybridWebSocketPacket> {
	type: HybridWebSocketPacketType;
	packet: T;
}
