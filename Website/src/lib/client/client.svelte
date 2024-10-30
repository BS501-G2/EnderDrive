<script lang="ts">
	import { onMount, type Snippet } from 'svelte';
	import { writable, type Writable } from 'svelte/store';
	import { createClientContext } from './client';
	import { Buffer } from 'buffer';

	const { children }: { children: Snippet } = $props();

	const {
		client,
		context: { request }
	} = createClientContext(async (data) => data);

	onMount(() => {
		let cancelled: boolean = false;

		void (async () => {
			while (!cancelled) {
				const data = await request(Buffer.alloc(100));

				// console.log(data);
			}
		})();

		return () => {
			cancelled = true;
		};
	});
</script>

{@render children()}

<!--
{#if promise != null || error != null}
	<div class="splash-container">
		<div class="splash">
			{#if error != null}
				<Icon icon="xmark" thickness="solid" size="2em" />

				<p>{error?.[0] ?? 'Unknown error'}</p>

				{#snippet container(view: Snippet)}
					<div class="retry-button">
						{@render view()}
					</div>
				{/snippet}
				 <Button {container} onclick={() => error?.[1]()}>Retry</Button>
			{:else}
				<LoadingSpinner size="82px" />
			{/if}
		</div>
	</div>
{:else}
	{@render children()}
{/if} -->

<style lang="scss">
	div.splash-container {
		flex-grow: 1;

		display: flex;

		align-items: center;
		justify-content: center;
	}

	div.splash {
		flex-direction: column;
		align-items: center;
		justify-content: center;

		gap: 8px;
	}

	div.retry-button {
		padding: 8px 16px;
	}
</style>
