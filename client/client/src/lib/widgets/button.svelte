<script lang="ts" module>
	import Awaiter, {
		type AwaiterResetFunction,
		type AwaiterChildrenParameters,

		AwaiterResultType

	} from '$lib/awaiter.svelte';

	export type ButtonClass =
		| 'primary'
		| 'primary-container'
		| 'background'
		| 'background-variant'
		| 'transparent';

	export type ButtonCallback = (event: MouseEvent) => Promise<void> | void;
</script>

<script lang="ts">
	import { type Snippet } from 'svelte';
	// import Awaiter, { AwaiterResultType, type AwaiterChildrenParameters, type AwaiterResetFunction } from "$lib/awaiter.svelte";

	import LoadingSpinner from './loading-spinner.svelte';
	import { writable, type Writable } from 'svelte/store';

	let {
		buttonClass = 'primary',
		loading: customLoading,
		container: customContainer,
		error: customError,
		enabled = true,
		outline = true,
		onClick,
		hint = null,
		children,
		click = $bindable()
	}: {
		children: Snippet;
		container?: Snippet<[content: Snippet]>;
		loading?: Snippet;
		error?: Snippet<[Error]>;

		buttonClass?: ButtonClass;
		enabled?: boolean;
		outline?: boolean;
		onClick: ButtonCallback;
		hint?: string | null;

		click?: () => void;
	} = $props();

	let busy: boolean = $state(false);
	let run: AwaiterResetFunction<null> | undefined = $state();

	let event: MouseEvent | null = null;
	let error: Writable<Error | null> = writable(null);

	async function buttonClick(newEvent: MouseEvent) {
		$error = null;
		try {
			event = newEvent;
			busy = true;

			await run?.(true, null);
		} finally {
			event = null;
			busy = false;
		}
	}

	let button: HTMLElement = $state(null as never);

	click = () => button?.click();
</script>

{#snippet buttonContent(props: AwaiterChildrenParameters<void, null>)}
	{#if props.status === AwaiterResultType.Loading}
		{#if customLoading != null}
			{@render customLoading()}
		{:else}
			<LoadingSpinner size="1em" />
		{/if}
	{:else if props.status === AwaiterResultType.Error}
		{#if customError != null}
			{@render customError(($error = (props as any).error))}
		{:else}
			{($error = (props as any).error).message}
		{/if}
	{:else}
		{@render children()}
	{/if}
{/snippet}

<button
	bind:this={button}
	disabled={!enabled || busy}
	class="button {buttonClass}"
	class:outline
	class:error={$error}
	onclick={buttonClick}
	title={hint}
>
	<Awaiter bind:reset={run} callback={() => onClick(event!)} autoLoad={false}>
		{#snippet children(props)}
			{#snippet content()}
				{@render buttonContent(props)}
			{/snippet}

			{#if customContainer != null}
				{@render customContainer(content)}
			{:else}
				{@render content()}
			{/if}
		{/snippet}
	</Awaiter>
</button>

<style lang="scss">
	button.button {
		// padding: 8px 16px 8px 16px;

		box-sizing: border-box;
		transition: all linear 150ms;

		border: solid 1px transparent;
		border-radius: 8px;

		cursor: pointer;
	}

	button.button.outline {
		border-color: var(--primary);
	}

	button.primary {
		background-color: var(--primary);
		color: var(--onPrimary);
	}

	button.primary:hover {
		background-color: var(--primaryVariant);
		color: var(--onPrimaryVariant);
	}

	button.primary:active {
		background-color: var(--onPrimary);
	}

	button.primary-container {
		background-color: var(--primaryContainer);
		color: var(--onPrimaryContainer);
	}

	button.primary-container:hover {
		background-color: var(--primaryContainerVariant);
		color: var(--onPrimaryContainer);
	}

	button.primary-container:active {
		background-color: var(--onPrimaryContainer);
		color: var(--onPrimary);
	}

	button.background {
		background-color: var(--background);
		color: var(--onBackground);
	}

	button.background:hover {
		background-color: var(--backgroundVariant);
		color: var(--onBackground);
	}

	button.background:active {
		background-color: var(--onBackground);
		color: var(--background);
	}

	button.background-variant {
		background-color: var(--backgroundVariant);
		color: var(--onBackground);
	}

	button.background-variant:hover {
		background-color: var(--background);
		color: var(--onBackground);
	}

	button.background-variant:active {
		background-color: var(--onBackground);
		color: var(--backgroundVariant);
	}

	button.transparent {
		background-color: transparent;
		color: inherit;
	}

	button.transparent:active {
		background-color: var(--shadow);
	}

	button:disabled {
		cursor: not-allowed;
	}

	button:disabled.primary {
		background-color: var(--primary);
		color: var(--onPrimary);
	}

	button:disabled.primary-container {
		background-color: var(--primaryContainer);
		color: var(--onPrimaryContainer);
	}

	button:disabled.background {
		background-color: var(--background);
		color: var(--onBackground);
	}

	button.error {
		background-color: var(--error);
		color: var(--onError);
	}

	button.error:hover {
		background-color: var(--errorBackground);
		color: var(--error);
	}

	button.error:active {
		background-color: var(--onError);
		color: var(--error);
	}
</style>
