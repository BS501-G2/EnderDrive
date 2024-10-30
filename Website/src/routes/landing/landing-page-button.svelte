<script lang="ts">
	import { onMount, type Snippet } from 'svelte';
	import { derived, writable, type Readable, type Writable } from 'svelte/store';

	const {
		id,
		name,
		isActive,
		onclick,
		widths
	}: {
		id: number;
		name: string;
		isActive: boolean;
		onclick: () => void;
		widths: Writable<[number, Readable<number>, Readable<number>][]>;
	} = $props();

	const button: Writable<HTMLButtonElement> = writable(null as never);

	onMount(() => {
		const obj: [number, Readable<number>, Readable<number>] = [
			id,
			derived(button, (button) => button.offsetLeft - button.parentElement!.offsetLeft),
			derived(button, (button) => button.clientWidth)
		];

		widths.update((value) => [...value, obj]);

		return () => widths.update((value) => value.filter((value) => value != obj));
	});
</script>

<button bind:this={$button} class:active={isActive} {onclick}>
	{name}
</button>

<style lang="scss">
	button {
		background: none;
		border: none;

		box-sizing: border-box;
		font-size: 1rem;

		cursor: pointer;
	}

	button.active {
		// font-weight: bold;
	}
</style>
