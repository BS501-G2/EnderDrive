<script lang="ts">
	import { range } from '$lib/range';
	import type { Writable } from 'svelte/store';

	const { currentIndex, count }: { currentIndex: Writable<number>; count: number } = $props();
</script>

<div class="counts">
	{#each range(count) as index}
		<button
			class:active={index == $currentIndex}
			onclick={() => {
				currentIndex.set(index);
			}}
			aria-label="Banner #{index + 1}"
		>
		</button>
	{/each}
</div>

<style lang="scss">
	div.counts {
		display: flex;
		flex-direction: row;

		align-items: center;

		padding: 8px;
		border-radius: 16px;
		gap: 8px;

		background-color: var(--background);

		> button {
			background-color: var(--primaryContainer);

			border: none;
			outline: none;

			min-width: 8px;
			max-width: 8px;
			min-height: 8px;
			max-height: 8px;

			padding: 0;

			border-radius: 50%;
		}

		> button.active {
			background-color: var(--primary);
		}
	}
</style>
