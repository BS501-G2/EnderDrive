import * as SocketIO from "socket.io";
import * as BSON from "bson";
import HTTP from "http";

export async function main() {
  const externalHttpServer = HTTP.createServer();
  const externalWebSocketServer = new SocketIO.Server({
    cors: {
      origin: "*",
    },
    path: "/ws",
  });

  externalHttpServer.on("request", (request, response) => {
    if (request.url == null || request.url == "/ws") {
      return;
    }

    HTTP.request(
      new URL(`http://localhost:8082${request.url}`),
      request,
      (internalResponse) => {
        response.statusCode = internalResponse.statusCode ?? 200;

        if (internalResponse.headers) {
          for (const [key, value] of Object.entries(internalResponse.headers)) {
            if (Array.isArray(value)) {
              for (const v of value) {
                response.setHeader(key, v);
              }
            } else if (typeof value === "string") {
              response.setHeader(key, value);
            }
          }
        }

        internalResponse.pipe(response);
      }
    );
  });

  externalWebSocketServer.on("connection", (externalSocket) => {
    const internalWebSocket = new WebSocket("ws://localhost:8082/ws");

    internalWebSocket.binaryType = "arraybuffer";

    let webSocketConnected = true;
    let socketConnected = true;

    internalWebSocket.onclose = (event) => {
      webSocketConnected = false;

      if (socketConnected) {
        externalSocket.disconnect(true);
      }
    };

    externalSocket.once("disconnect", () => {
      socketConnected = false;

      if (webSocketConnected) {
        internalWebSocket.close();
      }
    });

    internalWebSocket.onmessage = (event) => {
      const data: Uint8Array = new Uint8Array(event.data);

      externalSocket.emit("message", {
        type: data[0],
        packet: BSON.deserialize(data.slice(1)).Packet,
      });
    };

    externalSocket.on("message", (message: { type: number; packet: any }) =>
      internalWebSocket.send(
        new Uint8Array(
          concat(
            new Uint8Array([message.type]),
            BSON.serialize({
              Signature: "RizzziGit HybridWebSocketPacket2",
              Packet: BSON.serialize(message.packet),
            })
          )
        )
      )
    );
  });

  externalHttpServer.listen(8083);
  externalWebSocketServer.listen(externalHttpServer, {
    path: "/ws",
  });
}

function concat(...arrays: Uint8Array[]) {
  let totalLength = 0;
  for (const array of arrays) {
    totalLength += array.length;
  }

  const buffer = new Uint8Array(totalLength);

  let offset = 0;
  for (const array of arrays) {
    buffer.set(array, offset);
    offset += array.length;
  }

  return buffer;
}

await main();
