<script lang="ts" module>
	import { getContext, onMount, setContext, type Snippet } from 'svelte';
	import type { Readable } from 'svelte/motion';
	import { derived, get, readable, writable, type Writable } from 'svelte/store';
</script>

<script lang="ts">
	import Client from '$lib/client/client.svelte';
	import 'reset-css/reset.css';
	import '@fortawesome/fontawesome-free/css/all.min.css';
	import { fade } from 'svelte/transition';
	import { createColorContext } from '../lib/client/contexts/colors';
	import { createAppContext, ViewMode, WindowMode } from '$lib/client/contexts/app';

	const viewMode = writable<ViewMode>(ViewMode.None);
	const windowMode = writable<WindowMode>(WindowMode.Normal);
	const overlay: Writable<[id: number, snippet: Snippet][]> = writable([]);

	function triggerUpdateViewMode(window: Window) {
		let newMode: ViewMode = get(viewMode);

		if (window.innerWidth >= 1280) {
			newMode = ViewMode.Desktop;
		} else if (window.innerWidth >= 768) {
			newMode = ViewMode.LimitedDesktop;
		} else if (window.innerWidth < 768) {
			newMode = ViewMode.Mobile;
		} else {
			newMode = ViewMode.None;
		}

		if (newMode !== get(viewMode)) {
			viewMode.set(newMode);
		}
	}

	function triggerUpdateWindowMode(window: Window) {
		let newMode: WindowMode = get(windowMode);

		if (window.matchMedia('(display-mode: window-controls-overlay)').matches) {
			newMode = WindowMode.CustomBar;
		} else if (window.matchMedia('(display-mode: fullscreen)').matches) {
			newMode = WindowMode.Fullscreen;
		} else {
			newMode = WindowMode.Normal;
		}

		if (newMode !== get(windowMode)) {
			windowMode.set(newMode);
		}
	}

	const {} = createAppContext(viewMode, windowMode, overlay);
	const { printStyleHTML } = createColorContext();

	const { children }: { children: Snippet } = $props();

	onMount(() => {
		triggerUpdateViewMode(window);
		triggerUpdateWindowMode(window);
	});
</script>

<svelte:head>
	<link rel="icon" href="favicon.svg" type="image/xml+svg" />
	<meta charset="utf-8" />
	<meta name="viewport" content="width=device-width, initial-scale=1" />
	{@html $printStyleHTML()}
</svelte:head>

<svelte:window
	onresize={() => {
		triggerUpdateViewMode(window);
		triggerUpdateWindowMode(window);
	}}
/>

<Client>
	{@render children()}
</Client>

{#if $overlay.length != 0}
	<div class="overlay" transition:fade={{ duration: 250 }}>
		{#each $overlay as [id, snippet] (id)}
			{@render snippet()}
		{/each}
	</div>
{/if}

<style lang="scss">
	@use './global.scss' as *;

	:root {
		line-height: 1rem;
	}

	:global(div) {
		display: flex;
		flex-direction: column;
	}

	:global(input, button) {
		background-color: inherit;
		color: inherit;
	}

	:global(body) {
		display: flex;
		flex-direction: column;

		@include force-size(&, 100dvh);
		min-width: 320px;

		background-color: var(--color-5);
		color: var(--color-1);

		min-height: 100dvh;
		user-select: none;
	}

	div.overlay {
		flex-direction: column;

		position: fixed;
		top: 0;
		left: 0;

		z-index: 1;

		backdrop-filter: blur(8px);

		@include force-size(100dvw, 100dvh);
	}
</style>
