import * as SocketIO from "socket.io";
import * as BSON from "bson";
import HTTP from "http";

export async function main() {
  const externalHttpServer = HTTP.createServer();
  const externalWebSocketServer = new SocketIO.Server({
    cors: {
      origin: "*",
    },
    maxHttpBufferSize: 1024 * 1024 * 256,
  });

  externalWebSocketServer.on("connection", (externalSocket) => {
    const internalWebSocket = new WebSocket("ws://localhost:8082");

    const toInternalSend: Buffer[] = [];

    internalWebSocket.binaryType = "arraybuffer";

    let webSocketConnected = true;
    let socketConnected = true;

    internalWebSocket.onclose = () => {
      console.log("Internal WebSocket connection has been closed.");
      webSocketConnected = false;

      if (socketConnected) {
        console.log("Closing External WebSocket...");
        externalSocket.disconnect(true);
      }
    };

    externalSocket.on("error", () => {
      internalWebSocket.close();
    });

    internalWebSocket.onerror = () => {
      externalSocket.disconnect(true);
    };

    externalSocket.once("disconnect", () => {
      console.log("External WebSocket connection has been closed.");
      socketConnected = false;

      if (webSocketConnected) {
        console.log("Closing Internal WebSocket...");
        internalWebSocket.close();
      }
    });

    internalWebSocket.onopen = () => {
      for (let index = 0; index < toInternalSend.length; index++) {
        const { [index--]: buffer } = toInternalSend;
        toInternalSend.splice(index, 1);

        internalWebSocket.send(buffer);
      }
    };

    internalWebSocket.onmessage = (event) => {
      const packet = ((data): Packet | null => {
        const type = data[0];

        const id = data.subarray(1, 5).readInt32LE();
        switch (type) {
          case PACKET_REQUEST:
            return {
              type,
              id,
              data: { Code: data[5], Data: bsonDeserialize(data.subarray(6)) },
            };

          case PACKET_REQUEST_CANCEL:
            return {
              type,
              id,
            };

          case PACKET_RESPONSE:
            return {
              type,
              id,
              data: { Code: data[5], Data: bsonDeserialize(data.subarray(6)) },
            };

          case PACKET_RESPONSE_CANCEL:
            return {
              type,
              id,
            };

          case PACKET_RESPONSE_ERROR:
            return {
              type,
              id,
              error: data.subarray(5).toString("utf-8"),
            };

          case PACKET_RESPONSE_INTERNAL_ERROR:
            return {
              type,
              id,
              reason: data[5],
            };

          default:
            return null;
        }
      })(Buffer.from(new Uint8Array(event.data)));

      if (packet == null) {
        return;
      }

      externalSocket.emit("message", packet);
    };

    externalSocket.on("message", (packet: Packet) => {
      const data = ((packet) => {
        const buffer: Buffer[] = [Buffer.from([packet.type])];

        {
          const id = Buffer.alloc(4);

          id.writeInt32LE(packet.id);
          buffer.push(id);
        }

        switch (packet.type) {
          case PACKET_REQUEST: {
            buffer.push(Buffer.from([packet.data.Code]));
            buffer.push(bsonSerialize(packet.data.Data));
            break;
          }

          case PACKET_RESPONSE: {
            buffer.push(Buffer.from([packet.data.Code]));
            buffer.push(bsonSerialize(packet.data.Data));
            break;
          }

          case PACKET_RESPONSE_ERROR: {
            buffer.push(Buffer.from(packet.error, "utf-8"));
            break;
          }

          case PACKET_RESPONSE_INTERNAL_ERROR: {
            buffer.push(Buffer.from([packet.reason]));
          }
        }

        return Buffer.concat(buffer);
      })(packet);

      if (internalWebSocket.readyState !== WebSocket.OPEN) {
        toInternalSend.push(data);

        return;
      }

      internalWebSocket.send(data);
    });
  });

  externalHttpServer.listen(8083);
  externalWebSocketServer.listen(externalHttpServer);
}

const PACKET_REQUEST = 0 as const;
const PACKET_REQUEST_CANCEL = 1 as const;
const PACKET_RESPONSE = 2 as const;
const PACKET_RESPONSE_CANCEL = 3 as const;
const PACKET_RESPONSE_ERROR = 4 as const;
const PACKET_RESPONSE_INTERNAL_ERROR = 5 as const;

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

function bsonDeserialize(data: Buffer) {
  return BSON.deserialize(data);
}

function bsonSerialize(data: any): Buffer {
  return Buffer.from(BSON.serialize(data));
}

await main();
