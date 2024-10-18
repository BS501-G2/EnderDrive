<script lang="ts">
	import { type Writable, writable } from 'svelte/store';
	import { type Snippet } from 'svelte';
	import Bulletin from './bulletin.svelte';
	import LoginForm from './login-form.svelte';
	import Dialog from '$lib/widgets/dialog.svelte';
	import Button from '$lib/widgets/button.svelte';
	import { ViewMode, viewMode } from '$lib/responsive-layout.svelte';

	const {}: {} = $props();

	const errorStore: Writable<Error | null> = writable(null);
</script>

{#snippet buttonContainer(view: Snippet)}
	<div class="button-container">{@render view()}</div>
{/snippet}

{#if $errorStore != null}
	<Dialog
		onDismiss={() => {
			$errorStore = null;
		}}
		dialogClass="error"
	>
		{#snippet head()}
			<h2>Error</h2>
		{/snippet}
		{#snippet body()}
			{$errorStore!.message}
		{/snippet}
		{#snippet actions()}
			<Button
				onClick={() => {
					$errorStore = null;
				}}
				container={buttonContainer}
			>
				Close
			</Button>
		{/snippet}
	</Dialog>
{/if}

<div class="page">
	<div class="top-bar"></div>

	<div class="container" class:mobile={$viewMode & ViewMode.Mobile}>
		{#if $viewMode & ViewMode.Desktop}
			<Bulletin />
		{/if}

		<LoginForm {errorStore} />
	</div>
</div>

<style lang="scss">
	:global(body) {
		display: flex;
		flex-direction: row;
		justify-content: safe center;
	}

	div.page {
		display: flex;
		flex-direction: column;
		align-items: center;

		min-width: 100vw;
		max-width: 100vw;

		min-height: 100vh;
		max-height: 100vh;
	}

	div.top-bar {
		min-height: env(titlebar-area-height);

		background-color: var(--primaryContainer);
	}

	div.container {
		-webkit-app-region: no-drag;

		max-width: min(1280px, 100%);
		width: 100%;
		min-width: 0px;
		box-sizing: border-box;

		flex-grow: 1;
		display: flex;
		flex-direction: row;

		align-items: stretch;

		padding: 16px;
		gap: 16px;
	}

	div.container.mobile {
		padding: unset;
	}

	div.button-container {
		padding: 8px;
	}
</style>
