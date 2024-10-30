<script lang="ts">
	import type { Snippet } from 'svelte';
	import LoadingSpinner from './loading-spinner.svelte';

	const clickButton = () => {
		button.click();
	};

	let {
		children,
		container,
		onclick,
		click = $bindable()
	}: {
		children: Snippet;
		container?: Snippet<[content: Snippet]>;
		onclick: (
			event: MouseEvent & { currentTarget: EventTarget & HTMLButtonElement }
		) => void | Promise<void>;
		click?: () => void;
	} = $props();

	$effect(() => (click = clickButton));

	let promise: Promise<void> | null = $state(null);
	let error: Error | null = $state(null);

	$effect(() => {
		if (error != null) {
			console.log(error);
		}
	});

	let button: HTMLButtonElement = $state(null as never);
</script>

<button
	bind:this={button}
	onclick={(event) => {
		try {
			if (promise != null) {
				return;
			}

			const resultPromise = onclick(event);

			if (resultPromise instanceof Promise) {
				promise = resultPromise;

				void (async () => {
					try {
						await promise;
					} catch (e: any) {
						error = e;
					} finally {
						promise = null;
					}
				})();
			}
		} catch (e: any) {
			error = e;
		}
	}}
>
	<div class="background" class:error={error != null} class:busy={promise != null}>
		{#snippet content()}
			{#if error != null}
				{error.message}
			{:else if promise != null}
				<LoadingSpinner size="1em" />
			{:else}
				{@render children()}
			{/if}
		{/snippet}

		{#if container != null}
			{@render container(content)}
		{:else}
			{@render content()}
		{/if}
	</div>
</button>

<style lang="scss">
	button {
		display: flex;

		border: none;
		cursor: pointer;
		border-radius: 8px;
		padding: 0;

		overflow: hidden;

		transition-property: scale;

		div.background.error {
			background-color: var(--color-6);
		}

		div.background.busy {
			cursor: not-allowed;
		}

		div.background {
			transition-property: background-color, color;

			flex-grow: 1;
			height: 100%;
		}

		div.background.error {
			background-color: var(--color-6);
		}

		div.background.busy {
			cursor: not-allowed;
		}
	}

	div.background,
	button {
		transition: linear;
		transition-duration: 150ms;
	}

	button:hover {
		div.background {
			background-color: rgba(0, 0, 0, 0.25);
		}
	}

	button:focus {
		div.background {
			outline: #ffffff;
		}
	}

	button:active {
		scale: 0.95;

		div.background {
			background-color: rgba(0, 0, 0, 0.75);
			color: var(--color-5);
		}
	}
</style>
