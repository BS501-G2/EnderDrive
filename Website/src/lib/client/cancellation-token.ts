export class CancellationTokenSource {
	public constructor() {
		this.#binding = {
			requested: false,
			registrations: []
		};
	}

	readonly #binding: CancellationTokenBinding;

	public get token(): CancellationToken {
		return new CancellationToken(this.#binding);
	}

	public cancel(): void {
		this.#binding.requested = true;

        for (const registration of this.#binding.registrations) {
            registration();
        }
	}
}

export interface CancellationTokenBinding {
	requested: boolean;

	registrations: (() => void)[];
}

export class CancellationToken {
	public constructor(binding: CancellationTokenBinding) {
		this.#binding = binding;
	}

	readonly #binding: CancellationTokenBinding;

	public get requested(): boolean {
		return this.#binding.requested;
	}

	public register(callback: () => void): () => void {
		const registration = () => {
			callback();
		};

		this.#binding.registrations.push(registration);

		return () => {
			const index = this.#binding.registrations.indexOf(registration);

			if (index >= 0) {
				this.#binding.registrations.splice(index, 1);
			}
		};
	}
}
