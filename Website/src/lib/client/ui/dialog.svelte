<script lang="ts">
	import type { Snippet } from 'svelte';
	import Overlay from './overlay.svelte';

	const { head, children, footer }: { head?: Snippet; footer?: Snippet; children?: Snippet } =
		$props();
</script>

<Overlay>
	<div class="dialog">
		{#if head}
			<div class="head">
				{@render head()}
			</div>
		{/if}

		{#if children}
			<div class="body">
				{@render children()}
			</div>
		{/if}

		{#if footer}
			<div class="footer">
				{@render footer()}
			</div>
		{/if}
	</div>
</Overlay>

<style lang="scss">
	div.dialog {
		display: flex;
		flex-direction: column;

		padding: 8px;

		box-shadow: 2px 2px 8px black;
		background-color: white;

		border-radius: 8px;
		padding: 16px;
		gap: 16px;

		> div.head,
		> div.footer,
		> div.body {
			gap: 8px;
		}

		> div.head,
		> div.footer {
			flex-direction: row;
		}
	}
</style>
