import SocketIO from "socket.io";
import BSON from "bson";
import HTTP from "http";

export async function main() {
  const httpServer = HTTP.createServer();
  const externalServer = new SocketIO.Server({
    cors: {
      origin: "*",
    },
    path: "/ws",
  });

  httpServer.on("request", (request, response) => {
    if (request.url == null) {
      return;
    }

    const url = new URL(request.url);
    if (url.pathname == "/ws") {
      return;
    }

    HTTP.request(
      new URL(`http://localhost:8083${request.url}`),
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

  externalServer.on("connection", (externalSocket) => {
    const internalWebSocket = new WebSocket("ws://localhost:8083/ws");

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
        packet: BSON.deserialize(data.slice(1)),
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

  httpServer.listen(8082);
  externalServer.listen(httpServer, {
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
