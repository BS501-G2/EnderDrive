<script lang="ts" module>
	import { get, writable, type Writable } from 'svelte/store';

	export const ViewMode = {
		Unset: 0,

		Mobile: 0b0000001,
		Desktop: 0b0000010,
		Browser: 0b0000100,
		Standalone: 0b0001000,
		Fullscreen: 0b0010000,
		MinimalUI: 0b0100000,
		OverlayControls: 0b1000000
	};

	export const viewMode: Writable<number> = writable(ViewMode.Unset);

	function onResize() {
		let newViewMode: number = ViewMode.Unset;

		newViewMode |= window.matchMedia('(max-width: 768px)').matches
			? ViewMode.Mobile
			: ViewMode.Desktop;

		newViewMode |= window.matchMedia('(display-mode: minimal-ui)').matches
			? ViewMode.MinimalUI
			: window.matchMedia('(display-mode: window-controls-overlay)').matches
				? ViewMode.OverlayControls
				: window.matchMedia('(display-mode: standalone)').matches
					? ViewMode.Standalone
					: ViewMode.Browser;

		if (newViewMode != get(viewMode)) {
			viewMode.set(newViewMode);
		}
	}
</script>

<script lang="ts">
	import { onMount } from 'svelte';

	onMount(() => onResize());
</script>

<svelte:window onresize={onResize} />
