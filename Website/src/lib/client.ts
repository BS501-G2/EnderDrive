import type { Writable } from 'svelte/store';
import type { PromiseSource } from './promise-source';
import type { Readable } from 'svelte/motion';
import type { ClientStream } from './client/client-stream';

export interface Pending {
	id: number;

	source: PromiseSource<Uint8Array>;
}

export const CLIENT_STATUS_DISCONNECTED = 0b0001;
export const CLIENT_STATUS_CONNECTING = 0b0010;
export const CLIENT_STATUS_CONNECTED = 0b0100;

export type ClientStatus =
	| typeof CLIENT_STATUS_DISCONNECTED
	| typeof CLIENT_STATUS_CONNECTING
	| typeof CLIENT_STATUS_CONNECTED;

export interface ClientContext {
	request: () => [request: ClientStream, response: ClientStream];
	message: () => ClientStream;

	status: Writable<ClientStatus>;
	readOnlyStatus: Readable<ClientStatus>;
}

export class Client {
	public constructor() {
		void this.#start();
	}

	#context: () => ClientContext = () => {
		throw new Error('Not initialized');
	};

	async #start(): Promise<void> {
		while (true) {
			const oldGetContext = this.#context;

			try {
				let set: boolean = false;

				await this.#run((context) => {
					if (set) {
						throw new Error('Already set');
					}

					this.#context = () => context;
					set = true;
				});
			} finally {
				this.#context = oldGetContext;
			}

			await new Promise<void>((resolve) => setTimeout(resolve, 1000));
		}
	}

	async #run(setContext: (data: ClientContext) => void): Promise<void> {
		setContext({
			request: () => {}
		});

		while (true) {
			await this.#receive()
		}
	}

	async #receive() {}
}

let client: Client | null = null;

export const getClient = () => (client ??= new Client());
