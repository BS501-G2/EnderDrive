<script lang="ts">
	import { onMount, type Snippet } from 'svelte';
	import { useAppContext } from '$lib/client/contexts/app';
	import { scale } from 'svelte/transition';
	import { createOverlayContext } from '../lib/client/contexts/overlay';
	import Button from '../lib/client/ui/button.svelte';
	import Icon from '../lib/client/ui/icon.svelte';

	const { pushOverlayContent } = useAppContext();
	const {
		children,
		ondismiss,
		nodim = false
	}: {
		children: Snippet<[windowButtons: Snippet]>;
		ondismiss?: () => void;
		nodim?: boolean;
	} = $props();

	onMount(() => pushOverlayContent(overlay, !nodim));

	const {
		buttons,
		context: { pushButton }
	} = createOverlayContext();

	onMount(() => pushButton('test', { icon: 'xmark', thickness: 'solid' }, () => ondismiss?.()));
</script>

{#snippet windowButtons()}
	<div class="window-buttons">
		{#each $buttons as { id, tooltip, icon, onclick }, index (id)}
			{#snippet background(view: Snippet)}
				<div class="background">
					{@render view()}
				</div>
			{/snippet}

			{#snippet foreground(view: Snippet)}
				<div class="window-button">
					{@render view()}
				</div>
			{/snippet}

			<Button {onclick} {background} {foreground}>
				<Icon {...icon} size="1em" />
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
	@use '../global.scss' as *;

	div.window-buttons {
		flex-direction: row-reverse;

		div.background {
			flex-grow: 1;
		}

		div.window-button {
			padding: 8px 16px;
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
