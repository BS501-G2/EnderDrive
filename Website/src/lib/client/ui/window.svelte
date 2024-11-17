<script
	lang="ts"
>
	import type { Snippet } from 'svelte';
	import Overlay from '../../../routes/overlay.svelte';
	import Separator from './separator.svelte';

	const {
		title,
		header,
		children:
			body,
		ondismiss
	}: {
		title?: string;
		header?: Snippet;
		children: Snippet;
		ondismiss: () => void;
	} = $props();
</script>

<Overlay
	{ondismiss}
>
	{#snippet children(
		windowButtons: Snippet
	)}
		<div
			class="window"
		>
			<div
				class="header"
			>
				<h2
					class="title"
				>
					{title ??
						''}
				</h2>

				{#if header}
					<div
						class="head-bar"
					>
						{@render header()}
					</div>
				{/if}

				{@render windowButtons()}
			</div>

			<Separator
				horizontal
			/>

			<div
				class="body"
			>
				{@render body()}
			</div>
		</div>
	{/snippet}
</Overlay>

<style
	lang="scss"
>
	div.window {
		background-color: var(
			--color-9
		);
		color: var(
			--color-1
		);

		> div.header {
			gap: 8px;

			flex-direction: row;

			align-items: center;

			> h2.title {
				flex-grow: 1;
				font-size: 1.2em;
				font-weight: bolder;
				margin-left: 8px;
			}
		}

		> div.body {
			padding: 8px;
			gap: 8px;
		}
	}
</style>
