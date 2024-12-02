import * as SocketIO from "socket.io";
import * as BSON from "bson";

export async function main() {
  const server = new SocketIO.Server({
    cors: {
      origin: "*",
    },
    maxHttpBufferSize: 1024 * 1024 * 64,
  });

  server.on("connection", async (socket) => {
    let socketClosed = false;
    function closeSocket() {
      if (socketClosed) {
        return;
      }

      socketClosed = true;
      socket.disconnect(true);
    }

    let webSocket: WebSocket;

    try {
      webSocket = new WebSocket("ws://localhost:8082");
      webSocket.binaryType = "arraybuffer";
    } catch (error: any) {
      closeSocket();

      return;
    }

    let webSocketClosed = false;
    function closeWebSocket() {
      if (webSocketClosed) {
        return;
      }

      webSocketClosed = true;
      webSocket.close();
    }

    webSocket.onerror = () => closeSocket();
    webSocket.onclose = () => closeSocket();

    socket.on("disconnect", () => closeWebSocket());

    const prereceive: ConnectionPacket[] = [];
    const onprereceive = (data: ConnectionPacket) => prereceive.push(data);

    socket.on("", onprereceive);

    webSocket.onopen = () => {
      socket.off("", onprereceive);
      for (const entry of prereceive) {
        receivePacket(entry);
      }
      prereceive.splice(0, prereceive.length);

      socket.on("", (data: ConnectionPacket) => receivePacket(data));

      webSocket.onmessage = ({ data }: { data: ArrayBuffer }) =>
        receiveInternalPacket(
          BSON.deserialize(new Uint8Array(data), {
            promoteBuffers: true,
          }) as InternalConnectionPacket
        );
    };

    function sendPacket(packet: ConnectionPacket) {
      socket.emit("", packet);
    }

    function receivePacket(packet: ConnectionPacket) {
      if (packet.type === "error") {
        const { id, message, stack, type } = packet;

        sendInternalPacket({
          id,
          message,
          stack,
          type,
        });
      } else if (packet.type === "request") {
        const { id, name, data, type } = packet;

        sendInternalPacket({
          id,
          name,
          data: BSON.serialize(data),
          type,
        });
      } else if (packet.type === "response") {
        const { id, data, type } = packet;

        sendInternalPacket({
          id,
          data: BSON.serialize(data),
          type,
        });
      }
    }

    function sendInternalPacket(packet: InternalConnectionPacket) {
      webSocket.send(BSON.serialize(packet));
    }

    function receiveInternalPacket(packet: InternalConnectionPacket) {
      if (packet.type === "error") {
        const { id, message, stack, type } = packet;

        sendPacket({ id, message, stack, type });
      } else if (packet.type === "request") {
        const { id, name, data, type } = packet;

        sendPacket({
          id,
          name,
          data: BSON.deserialize(Buffer.from(data)),
          type,
        });
      } else if (packet.type === "response") {
        const { id, data, type } = packet;

        sendPacket({
          id,
          data: BSON.deserialize(Buffer.from(data), { promoteBuffers: true }),
          type,
        });
      }
    }
  });

  server.listen(8083);
}

export type InternalConnectionPacket = {
  id: number;
} & (
  | {
      type: "request";
      name: string;
      data: Uint8Array;
    }
  | {
      type: "response";
      data: Uint8Array;
    }
  | {
      type: "error";
      message: string;
      stack?: string;
    }
);

export type ConnectionPacket = {
  id: number;
} & (
  | {
      type: "request";
      name: string;
      data: any;
    }
  | {
      type: "response";
      data: any;
    }
  | {
      type: "error";
      message: string;
      stack?: string;
    }
);

await main();
