<script lang="ts" module>
	export type AwaiterResetFunction<P> = (
		load?: boolean,
		initialPayload?: P | null
	) => Promise<void>;

	export type AwaiterCallback<T, P> = (setPayload: P) => T | Promise<T>;

	export const AwaiterResultType = Object.freeze({
		Ready: 0,
		Loading: 1,
		Success: 2,
		Error: 3
	});

	export type AwaiterChildrenParameters<T, P> = {
		status: number;
		payload: P | null;
		reset: AwaiterResetFunction<P>;
	} & (
		| {
				status: typeof AwaiterResultType.Success;
				result: T;
		  }
		| {
				status: typeof AwaiterResultType.Ready;
		  }
		| {
				status: typeof AwaiterResultType.Error;
				error: Error;
		  }
		| {
				status: typeof AwaiterResultType.Loading;
		  }
	);
</script>

<script lang="ts" generics="T extends any, P extends any">
	import { onMount, type Snippet } from 'svelte';

	let {
		callback,
		autoLoad = true,
		reset = $bindable(),

		initialPayload = null,

		ready,
		loading,
		children,
		success,
		error
	}: {
		callback: (setPayload: (payload: P | null) => void) => Promise<T> | T;
		autoLoad?: boolean;
		initialPayload?: P | null;
		reset?: AwaiterResetFunction<P>;

		children?: Snippet<[AwaiterChildrenParameters<T, P>]>;
		ready?: Snippet<[AwaiterChildrenParameters<T, P> & { status: typeof AwaiterResultType.Ready }]>;
		loading?: Snippet<
			[AwaiterChildrenParameters<T, P> & { status: typeof AwaiterResultType.Loading }]
		>;
		success?: Snippet<
			[AwaiterChildrenParameters<T, P> & { status: typeof AwaiterResultType.Success }]
		>;
		error?: Snippet<[AwaiterChildrenParameters<T, P> & { status: typeof AwaiterResultType.Error }]>;
	} = $props();

	let autoLoadState: boolean = $state(autoLoad);
	let payloadState: P | null = $state(initialPayload);
	let promiseState: Promise<T> | null = $state(null);

	async function run(): Promise<void> {
		try {
			const setPayload: (payload: P | null) => void = (payload) => (payloadState = payload);

			try {
				const result = callback(setPayload);

				if (result instanceof Promise) {
					try {
						await (promiseState = result);
					} catch {}
				}
			} catch (error: any) {
				throw (promiseState = Promise.reject(error));
			}
		} catch {}

		try {
			await promiseState;
		} catch (error: any) {
			console.error(error);
		}
	}

	const resetFunc: AwaiterResetFunction<P> = async (
		load: boolean = autoLoad,
		initialPayload: P | null = null
	) => {
		promiseState = null;
		payloadState = initialPayload;

		if ((autoLoadState = load)) {
			await run();
		}
	};

	reset = resetFunc;

	onMount(async () => {
		if (autoLoadState) {
			void run();
		}
	});
</script>

{#snippet readyWrapper()}
	{@const params: AwaiterChildrenParameters<T, P> & {  status: typeof AwaiterResultType.Ready } = { reset: resetFunc, status: AwaiterResultType.Ready, payload: payloadState }}
	{#if ready != null}
		{@render ready(params)}
	{:else if children != null}
		{@render children(params)}
	{/if}
{/snippet}

{#snippet loadingWrapper()}
	{@const params: AwaiterChildrenParameters<T, P> & { status: typeof AwaiterResultType.Loading } = { reset: resetFunc, status: AwaiterResultType.Loading, payload: payloadState }}
	{#if loading != null}
		{@render loading(params)}
	{:else if children != null}
		{@render children(params)}
	{/if}
{/snippet}

{#snippet successWrapper(result: T)}
	{@const params: AwaiterChildrenParameters<T, P> & { status: typeof AwaiterResultType.Success } = { reset: resetFunc, status: AwaiterResultType.Success, result, payload: payloadState }}
	{#if success != null}
		{@render success(params)}
	{:else if children != null}
		{@render children(params)}
	{/if}
{/snippet}

{#snippet errorWrapper(errorData: Error)}
	{@const params: AwaiterChildrenParameters<T, P> & { status: typeof AwaiterResultType.Error } = { reset: resetFunc, status: AwaiterResultType.Error, error: errorData, payload: payloadState }}
	{#if error != null}
		{@render error(params)}
	{:else if children != null}
		{@render children(params)}
	{/if}
{/snippet}

{#if promiseState == null}
	{#if autoLoad}
		{@render loadingWrapper()}
	{:else}
		{@render readyWrapper()}
	{/if}
{:else}
	{#await promiseState}
		{@render loadingWrapper()}
	{:then result}
		{@render successWrapper(result)}
	{:catch error}
		{@render errorWrapper(error)}
	{/await}
{/if}
