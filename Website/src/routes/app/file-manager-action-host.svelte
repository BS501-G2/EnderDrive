<script lang="ts">
	import { useFileBrowserContext } from '$lib/client/contexts/file-browser';
	import { onMount, type Snippet } from 'svelte';
	import type { Readable } from 'svelte/motion';

	const { actions }: { actions: Readable<{ id: number; snippet: Snippet }[]> } = $props();
	const { pushTop } = useFileBrowserContext();

	onMount(() => pushTop(desktopContent));
</script>

{#snippet c()}
	<div class="actions">
		{#each $actions as { id, snippet } (id)}
			{@render snippet()}
		{/each}
	</div>
{/snippet}

{#snippet desktopContent()}
<!-- {#if $}

{/if} -->
{/snippet}

<style lang="scss">
	@use '../../global.scss' as *;

	div.actions {
		@include force-size(&, 48px);

		flex-grow: 1;
	}
</style>
