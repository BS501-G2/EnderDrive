<script lang="ts" module>
	import { viewMode } from '$lib/responsive-layout.svelte';
	import { ViewMode } from '$lib/responsive-layout.svelte';

	export type DialogClass = 'normal' | 'warning' | 'error';
</script>

<script lang="ts">
	import { XIcon } from 'svelte-feather-icons';

	import Button from './button.svelte';
	import { scale } from 'svelte/transition';
	import Overlay from './overlay.svelte';
	import type { Snippet } from 'svelte';

	const {
		onDismiss,
		dialogClass = 'normal',
		head,
		body,
		actions,

		dialogContainer,
		headContainer,
		bodyContainer,
		actionsContainer
	}: {
		onDismiss: () => void;
		dialogClass?: DialogClass;
		head?: Snippet;
		body?: Snippet;
		actions?: Snippet;

		dialogContainer?: Snippet<[view: Snippet]>;
		headContainer?: Snippet<[view: Snippet]>;
		bodyContainer?: Snippet<[view: Snippet]>;
		actionsContainer?: Snippet<[view: Snippet]>;
	} = $props();
</script>

{#snippet dialog()}
	<div
		class="dialog {dialogClass} dim"
		class:mobile={$viewMode & ViewMode.Mobile}
		transition:scale|global={{ duration: 200, start: 0.9 }}
	>
		{#if head}
			{#if $viewMode & ViewMode.Desktop}
				{#if head}
					{#snippet headContent()}
						<div class="head">
							{@render head()}
						</div>
					{/snippet}

					{#if headContainer}
						{@render headContainer(headContent)}
					{:else}
						{@render headContent()}
					{/if}
				{/if}
			{:else if $viewMode & ViewMode.Mobile}
				<div class="head-mobile">
					<div class="head-element">
						{#if head}
							{#if headContainer}
								{@render headContainer(head)}
							{:else}
								{@render head()}
							{/if}
						{/if}
					</div>
					<Button buttonClass="transparent" onClick={() => onDismiss()} outline={false}>
						<div class="close-button">
							<XIcon />
						</div>
					</Button>
				</div>
			{/if}
		{/if}
		{#if body}
			{#snippet bodyContent()}
				<div class="body">
					{@render body()}
				</div>
			{/snippet}

			{#if bodyContainer}
				{@render bodyContainer(bodyContent)}
			{:else}
				{@render bodyContent()}
			{/if}
		{/if}
		{#if actions}
			{#snippet actionsContent()}
				<div class="actions">
					{@render actions()}
				</div>
			{/snippet}

			{#if actionsContainer}
				{@render actionsContainer(actionsContent)}
			{:else}
				{@render actionsContent()}
			{/if}
		{/if}
	</div>
{/snippet}

<Overlay {onDismiss} dim={true}>
	{#if dialogContainer}
		{@render dialogContainer(dialog)}
	{:else}
		{@render dialog()}
	{/if}
</Overlay>

<style lang="scss">
	div.dialog {
		max-width: calc(100dvw - 32px);
		max-height: calc(100dvh - 32px);

		box-sizing: border-box;

		padding: 16px;
		// border: solid 1px var(--primary);
		border-radius: 8px;
		box-shadow: var(--shadow) 2px 2px 8px;

		display: flex;
		flex-direction: column;
		justify-content: space-between;

		gap: 8px;

		div.head-mobile {
			display: flex;

			> div.head-element {
				flex-grow: 1;
			}
		}

		> div.head {
			text-align: left;
		}

		> div.body {
			flex-grow: 1;
			text-align: left;

			box-sizing: border-box;

			overflow-y: auto;
			overflow-x: hidden;
		}

		> div.actions {
			display: flex;
			align-items: center;

			gap: 8px;
			justify-content: flex-end;
		}
	}

	div.dialog.dim {
		box-shadow: black 2px 2px 8px;
	}

	div.dialog.mobile {
		width: 100dvw;
		height: 100dvh;

		max-width: 100dvw;
		max-height: 100dvh;

		padding: 16px;

		border: none;
		border-radius: 0px;
	}

	div.dialog.normal {
		background-color: var(--backgroundVariant);
		color: var(--onBackgroundVariant);
	}

	div.dialog.warning {
		background-color: #ffffa0;
		color: var(--onBackground);
	}

	div.dialog.error {
		background-color: #ffa0a0;
		color: var(--onBackground);
	}

	div.close-button {
		min-width: 100%;
		min-height: 100%;

		display: flex;
		flex-direction: row;
		align-items: center;
	}
</style>
