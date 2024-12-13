<script lang="ts" module>
	export type OverlayPositionType = 'center' | 'offset';

	export type OverlayPosition = [type: 'center'] | [type: 'offset', x: number, y: number];
</script>

<script lang="ts">
	import type { Snippet } from 'svelte';

	import type { MouseEventHandler } from 'svelte/elements';
	import { fade, scale } from 'svelte/transition';

	let element: HTMLElement | null = $state(null);

	const onClick: MouseEventHandler<HTMLButtonElement> = (event) => {
		if (element?.contains(event.target as HTMLElement)) {
			event.preventDefault();
			return;
		}

		onDismiss?.();
	};

	const {
		dim = false,
		position = ['center'],
		onDismiss,
		children,

		'no-pointer-events': noPointerEvents = false
	}: {
		dim?: boolean;
		position?: OverlayPosition;
		onDismiss?: (() => void) | null;
		children: Snippet;

		'no-pointer-events'?: boolean;
	} = $props();
</script>

<div class="content">
	{#if onDismiss != null}
		<div class="layer">
			{#if dim}
				<div
					class="view"
					transition:fade|global={{ duration: 200 }}
					style="background-color: var(--shadow);"
				>
					<button onclick={onClick}></button>
				</div>
			{:else}
				<div class="view">
					<button onclick={onClick}></button>
				</div>
			{/if}
		</div>
	{/if}
	<div class="layer">
		<div class="view">
			{#if position[0] === 'offset'}
				{@const overlayPositionX = Math.abs(position[1])}
				{@const overlayPositionY = Math.abs(position[2])}

				<div
					class="custom-offset"
					style="align-items: {position[1] < 0
						? 'flex-end'
						: 'flex-start'}; justify-content: {position[2] < 0 ? 'flex-end' : 'flex-start'}"
				>
					<div
						style="margin-{position[2] < 0
							? 'bottom'
							: 'top'}: {overlayPositionY}px; margin-{position[1] < 0
							? 'right'
							: 'left'}: {overlayPositionX}px; z-index: 2;"
						class="main"
						class:no-pointer-events={noPointerEvents}
						bind:this={element}
					>
						{@render children()}
					</div>
				</div>
			{:else if position[0] === 'center'}
				<div
					class="main"
					style="z-index: 2;"
					class:no-pointer-events={noPointerEvents}
					bind:this={element}
				>
					{@render children()}
				</div>
			{/if}
		</div>
	</div>
</div>

<style lang="scss">
	div.content {
		z-index: 1;

		-webkit-app-region: no-drag;

		display: flex;
		flex-direction: column;
		position: absolute;
		left: 0px;
		top: 0px;
		pointer-events: none;

		width: 100%;
		height: 100%;

		> div.layer {
			max-height: 0px;

			> div.view {
				min-height: 100dvh;

				display: flex;
				flex-direction: column;
				justify-content: center;
				align-items: center;

				background-color: transparent;

				> button,
				> div.custom-offset > div.main,
				> div.main {
					pointer-events: auto;
				}

				> div.main.no-pointer-events {
					pointer-events: none;
				}

				> div.custom-offset {
					width: 100dvw;
					height: 100dvh;
					display: inline;
					pointer-events: none;

					display: flex;
					flex-direction: column;

					overflow: hidden;

					> div.main {
						pointer-events: auto;

						display: flex;
						flex-direction: column;

						min-height: 0px;
						min-width: 0px;
					}

					> div.main.no-pointer-events {
						pointer-events: none;
					}
				}

				> button {
					width: 100dvw;
					height: 100dvh;
					border: none;
					background-color: transparent;
					outline: none;

					padding: 0px;

					display: flex;
				}
			}
		}
	}
</style>
