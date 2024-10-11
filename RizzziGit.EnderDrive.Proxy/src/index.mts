import SocketIO from "socket.io";
import BSON from "bson";
import HTTP from "http";

export async function main() {
  const httpServer = HTTP.createServer();
  const socketServer = new SocketIO.Server({
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

  socketServer.on("connection", (socket) => {
    const webSocket = new WebSocket("ws://localhost:8083/ws");

    webSocket.binaryType = "arraybuffer";

    let webSocketConnected = true;
    let socketConnected = true;

    webSocket.onclose = (event) => {
      webSocketConnected = false;

      if (socketConnected) {
        socket.disconnect(true);
      }
    };

    socket.once("disconnect", () => {
      socketConnected = false;

      if (webSocketConnected) {
        webSocket.close();
      }
    });

    webSocket.onmessage = (event) =>
      socket.emit(
        "message",
        BSON.deserialize(new Uint8Array(event.data)).Packet
      );

    socket.on("message", (message) =>
      webSocket.send(
        BSON.serialize({
          Signature: "RizzziGit HybridWebSocketPacket2",
          Packet: message,
        })
      )
    );
  });

  httpServer.listen(8082);
  socketServer.listen(httpServer, {
    path: "/ws",
  });
}

await main();
