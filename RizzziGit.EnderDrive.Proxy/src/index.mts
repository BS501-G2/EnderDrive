import * as SocketIO from "socket.io";
import * as BSON from "bson";

// const connections: Record<string, Connection> = {};

// export async function connectInternal() {
//   return await new Promise<Connection>((resolve, reject) => {
//     let webSocket: WebSocket = null as never;

//     try {
//       webSocket = new WebSocket("ws://localhost:8082");
//       webSocket.binaryType = "arraybuffer";
//     } catch (error: any) {
//       reject(error);
//       return;
//     }

//     webSocket.onerror = () => reject(new Error("Failed to connect."));
//     webSocket.onopen = () => {
//       const pendingRequests: Record<
//         string,
//         { resolve: (data: Uint8Array) => void; reject: (reason?: any) => void }
//       > = {};

//       const pendingResponses: Record<
//         string,
//         {
//           id: string;
//           name: string;
//           data: Uint8Array;
//           resolve: (data: Uint8Array) => void;
//           reject: (reason?: any) => void;
//         }
//       > = {};

//       let boundExternalRequest:
//         | ((name: string, data: Uint8Array) => Promise<Uint8Array>)
//         | null = null;

//       const forward = (
//         id: string,
//         name: string,
//         data: Uint8Array,
//         request: (name: string, data: Uint8Array) => Promise<Uint8Array>
//       ) => {
//         request(name, data)
//           .then(
//             (data) => {
//               const responsePacket: ConnectionPacket = {
//                 type: "response",
//                 id,
//                 data,
//               };

//               webSocket.send(bsonSerialize(responsePacket));
//             },
//             (error) => {
//               const responsePacket: ConnectionPacket = {
//                 type: "error",
//                 id,
//                 message: error.message,
//                 stack: error.stack,
//               };

//               webSocket.send(bsonSerialize(responsePacket));
//             }
//           )
//           .finally(() => {
//             delete pendingResponses[id];
//           });
//       };

//       const connection: Connection = {
//         id: BSON.ObjectId.generate(Date.now()).toString(),

//         webSocket,

//         request: (name: string, data: Uint8Array) =>
//           new Promise<Uint8Array>((resolve, reject) => {
//             const packet: InternalConnectionPacket = {
//               id: BSON.ObjectId.generate(Date.now()).toString(),
//               type: "request",
//               name,
//               data,
//             };

//             pendingRequests[packet.id] = { resolve, reject };
//             webSocket.send(bsonSerialize(packet));
//           }),

//         bind: (request) => {
//           boundExternalRequest = request;

//           for (const id in pendingResponses) {
//             const {
//               [id]: { name, data },
//             } = pendingResponses;

//             forward(id, name, data, boundExternalRequest);
//           }
//         },

//         unbind: () => {
//           boundExternalRequest = null;
//         },
//       };

//       connections[connection.id] = connection;
//       resolve(connection);

//       webSocket.onerror = console.error;
//       webSocket.close = () => {
//         delete connections[connection.id];

//         for (const id in pendingRequests) {
//           const {
//             [id]: { reject },
//           } = pendingRequests;

//           reject(new Error("Connection closed"));
//         }
//       };

//       webSocket.onmessage = ({ data }: { data: ArrayBuffer }) => {
//         const buffer = new Uint8Array(data);
//         const packet = bsonDeserialize(buffer) as ConnectionPacket;

//         if (packet.type === "request") {
//           void (async () => {
//             await new Promise<Uint8Array>(
//               (resolve: (data: Uint8Array) => void, reject) => {
//                 pendingResponses[packet.id] = {
//                   id: packet.id,
//                   name: packet.name,
//                   data: packet.data,
//                   resolve,
//                   reject,
//                 };

//                 if (boundExternalRequest == null) {
//                   return;
//                 } else {
//                   forward(
//                     packet.id,
//                     packet.name,
//                     packet.data,
//                     boundExternalRequest
//                   );
//                 }
//               }
//             );
//           })();
//         } else {
//           const pending = pendingRequests[packet.id];
//           delete pendingRequests[packet.id];

//           if (packet.type === "error") {
//             pending.reject(
//               Object.assign(new Error(packet.message), {
//                 stack: packet.stack,
//               })
//             );
//           } else if (packet.type === "response") {
//             pending.resolve(packet.data);
//           }
//         }
//       };
//     };
//   });
// }

// export interface Connection {
//   id: string;
//   webSocket: WebSocket;

//   request: (name: string, data: Uint8Array) => Promise<Uint8Array>;

//   bind: (
//     request: (name: string, data: Uint8Array) => Promise<Uint8Array>
//   ) => void;

//   unbind: () => void;
// }

// export class RewindError extends Error {}

// export type InternalConnectionPacket = {
//   id: string;
// } & (
//   | {
//       type: "request";
//       name: string;
//       data: Uint8Array;
//     }
//   | {
//       type: "response";
//       data: Uint8Array;
//     }
//   | {
//       type: "error";
//       message: string;
//       stack?: string;
//     }
// );

export async function main() {
  const server = new SocketIO.Server({
    cors: {
      origin: "*",
    },
    maxHttpBufferSize: 1024 * 512,
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

    socket.on("", (data: ConnectionPacket) => receivePacket(data));

    webSocket.onmessage = ({ data }: { data: ArrayBuffer }) =>
      receiveInternalPacket(
        bsonDeserialize(Buffer.from(data)) as InternalConnectionPacket
      );

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
          data: bsonSerialize(data),
          type,
        });
      } else if (packet.type === "response") {
        const { id, data, type } = packet;

        sendInternalPacket({
          id,
          data: bsonSerialize(data),
          type,
        });
      }
    }

    function sendInternalPacket(packet: InternalConnectionPacket) {
      webSocket.send(bsonSerialize(packet));
    }

    function receiveInternalPacket(packet: InternalConnectionPacket) {
      if (packet.type === "error") {
        const { id, message, stack, type } = packet;

        sendPacket({ id, message, stack, type });
      } else if (packet.type === "request") {
        const { id, name, data, type } = packet;

        sendPacket({ id, name, data: bsonDeserialize(data), type });
      } else if (packet.type === "response") {
        const { id, data, type } = packet;

        sendPacket({ id, data: bsonDeserialize(data), type });
      }
    }
  });

  server.listen(8083);
}

export type InternalConnectionPacket = {
  id: string;
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
  id: string;
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

function bsonDeserialize(data: Uint8Array) {
  return BSON.deserialize(data);
}

function bsonSerialize(data: any): Uint8Array {
  return Buffer.from(BSON.serialize(data));
}

await main();
