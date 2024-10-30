<script lang="ts">
	import { onMount, type Snippet } from 'svelte';
	import { useAppContext } from '$lib/client/contexts/app';
	import { scale } from 'svelte/transition';
	import { createOverlayContext } from '../contexts/overlay';
	import Button from './button.svelte';
	import Icon from './icon.svelte';

	const { pushOverlayContent } = useAppContext();
	const {
		children,
		ondismiss
	}: { children: Snippet<[windowButtons: Snippet]>; ondismiss?: () => void } = $props();

	onMount(() => pushOverlayContent(overlay));

	const {
		buttons,
		context: { pushButton }
	} = createOverlayContext();

	onMount(() => pushButton('test', { icon: 'xmark', thickness: 'solid' }, () => ondismiss?.()));
</script>

{#snippet windowButtons()}
	<div class="window-buttons">
		{#each $buttons as { id, tooltip, icon, onclick }, index (id)}
			{#snippet windowButtonContainer(view: Snippet)}
				<div class="window-button-container">
					{@render view()}
				</div>
			{/snippet}

			<Button {onclick} container={windowButtonContainer}>
				<div class="window-button">
					<Icon {...icon} size="1em" />
				</div>
			</Button>
		{/each}
	</div>
{/snippet}

{#snippet overlay()}
	<div class="overlay-bounds">
		<button
			class="overlay-container"
			transition:scale|global={{ duration: 250, start: 0.95 }}
			onclick={({ currentTarget, target }) => {
				if (currentTarget != target) {
					return;
				}

				ondismiss?.();
			}}
		>
			<div class="overlay">
				{@render children(windowButtons)}
			</div>
		</button>
	</div>
{/snippet}

<style lang="scss">
	@use '../../../routes/global.scss' as *;

	div.window-buttons {
		flex-direction: row-reverse;

		div.window-button-container {
			flex-grow: 1;
			padding: 8px 16px;
		}

		div.window-button {
		}
	}

	div.overlay-bounds {
		min-height: 0;
		max-height: 0;
	}

	button.overlay-container {
		display: flex;
		flex-direction: column;

		align-items: center;
		justify-content: center;

		border: none;
		outline: none;

		@include force-size(100dvw, 100dvh);
	}

	div.overlay {
		display: flex;
		flex-direction: column;

		min-width: 0px;
		min-height: 0px;
		max-width: 100dvw;
		max-height: 100dvh;
	}
</style>
