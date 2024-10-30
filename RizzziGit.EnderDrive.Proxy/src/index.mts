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
    console.log(request.url);

    if (request.url == null || request.url == "/ws") {
      return;
    }

    const internalRequest = HTTP.request(
      new URL(`http://localhost:8082${request.url}`),
      {
        method: request.method,
        headers: request.headers,
      },
      (internalResponse) => {
        internalResponse.pipe(response);
      }
    );

    request.pipe(internalRequest);
  });

  externalWebSocketServer.on("connection", (externalSocket) => {
    const internalWebSocket = new WebSocket("ws://localhost:8082/ws");

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

    externalSocket.once("disconnect", () => {
      console.log("External WebSocket connection has been closed.");
      socketConnected = false;

      if (webSocketConnected) {
        console.log("Closing Internal WebSocket...");
        internalWebSocket.close();
      }
    });

    internalWebSocket.onmessage = (event) => {
      const data: Uint8Array = new Uint8Array(event.data);
      const packet = {
        type: data[0],
        packet: BSON.deserialize(data.slice(1)),
      };

      console.log("<-", packet);
      externalSocket.emit("message", packet);
    };

    externalSocket.on("message", (message: { type: number; packet: any }) => {
      console.log("->", message);

      internalWebSocket.send(
        new Uint8Array(
          concat(new Uint8Array([message.type]), BSON.serialize(message.packet))
        )
      );
    });
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
