<script lang="ts">
	import { type Snippet } from 'svelte';
	import Overlay from '../overlay.svelte';
	import NotificationHost from './notification-host.svelte';
	import Separator from '$lib/client/ui/separator.svelte';


	const { element, ondismiss }: { element: HTMLElement; ondismiss: () => void } = $props();

	const boundElement = element.getBoundingClientRect();

	const x = -(1 + (window.innerWidth - (boundElement.x + boundElement.width)));
	const y = boundElement.y + boundElement.height;
</script>

<Overlay {ondismiss} {x} {y} nodim>
	{#snippet children(windowButtons: Snippet)}
		<div class="notification">
			<div class="header">
				<h2>Notifications</h2>

				{@render windowButtons()}
			</div>

			<Separator horizontal />

			<div class="main">
				<NotificationHost />
			</div>
		</div>
	{/snippet}
</Overlay>

<style lang="scss">
	@use '../../global.scss' as *;

	div.notification {
		background-color: var(--color-9);
		color: var(--color-1);

		filter: drop-shadow(2px 2px 2px var(--color-10));

		> div.header {
			flex-direction: row;
			align-items: center;

			> h2 {
				margin: 0 8px;
				font-weight: bolder;
				font-size: 1.2em;
				flex-grow: 1;
			}
		}

		> div.main {
			padding: 8px;

			@include force-size(min(calc(100dvw - 64px), 360px), min(calc(100dvh - 128px), 720px));
		}
	}
</style>
