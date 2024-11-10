<script lang="ts">
	import { onMount, type Snippet } from 'svelte';
	import { derived, type Readable } from 'svelte/store';
	import { fade } from 'svelte/transition';

	const { overlay }: { overlay: Readable<[id: number, snippet: Snippet<[]>, dim: boolean][]> } =
		$props();

	const dim = derived(overlay, (value) => value.some(([, , dim]) => dim));
</script>

{#if $overlay.length != 0}
	<div class="overlay" class:dim={$dim} transition:fade={{ duration: 250 }}>
		<div class:overlay-a={$dim}>
			{#each $overlay as [id, snippet] (id)}
				{@render snippet()}
			{/each}
		</div>
	</div>
{/if}

<style lang="scss">
	@use '../global.scss' as *;

	div.overlay {
		flex-direction: column;

		position: fixed;
		top: 0;
		left: 0;

		z-index: 1;

		@include force-size(100dvw, 100dvh);

		> div.overlay-a {
			flex-direction: column;

			filter: drop-shadow(2px 2px 2px black);
		}
	}

	div.overlay.dim {
		backdrop-filter: blur(8px);
	}
</style>
