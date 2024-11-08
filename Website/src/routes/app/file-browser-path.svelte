<script lang="ts">
	import { useClientContext, useServerContext } from '$lib/client/client';
	import { useFileBrowserContext, type CurrentFile } from '$lib/client/contexts/file-browser';
	import Button from '$lib/client/ui/button.svelte';
	import Icon from '$lib/client/ui/icon.svelte';
	import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte';
	import { onMount, type Snippet } from 'svelte';

	const { getFilePath } = useServerContext();
	const { pushTop, onFileId } = useFileBrowserContext();
	const { whoAmI } = useServerContext();

	const { current }: { current: CurrentFile & { type: 'file' | 'folder' | 'loading' } } = $props();

	onMount(() => pushTop(content));
</script>

{#snippet content()}
	{#snippet loading()}
		<div class="loading">
			<LoadingSpinner size="1em" />
			<p>Loading</p>
		</div>
	{/snippet}

	<div class="path">
		{#await whoAmI()}
			{@render loading()}
		{:then me}
			{#if current.type === 'loading' || me == null}
				{@render loading()}
			{:else}
				{@const root = current.path[0]}

				{#snippet rootForeground(view: Snippet)}
					<div class="root">
						{@render view()}
					</div>
				{/snippet}

				{#snippet rootBackground(view: Snippet)}
					<div class="root-background">
						{@render view()}
					</div>
				{/snippet}

				<Button
					background={rootBackground}
					foreground={rootForeground}
					onclick={(event) => {
						onFileId?.(event, root.id);
					}}
				>
					{#if root.ownerUserId}
						<Icon icon="folder" />
						<p>My Files</p>
					{:else}
						<Icon icon="user" />
						<p>Root</p>
					{/if}
				</Button>

				{#each current.path.splice(1) as file (file.id)}
				<Button onclick={() => {}}>
					<p>asd</p>
				</Button>
					<div class="entry-list">

					</div>
				{/each}
			{/if}
		{/await}
	</div>
{/snippet}

<style lang="scss">
	@use '../../global.scss' as *;

	div.path {
		@include force-size(&, 32px);

		gap: 8px;

		flex-direction: row;

		> div.loading {
			padding: 0 8px;
			gap: 8px;

			flex-direction: row;
			align-items: center;
		}

		div.root-background {
			flex-direction: row;
			align-items: center;

			flex-grow: 1;

			background-color: var(--color-5);
			color: var(--color-1);
		}

		div.root {
			flex-grow: 1;

			align-items: center;
			flex-direction: row;

			padding: 0 8px;
			gap: 8px;
		}
	}
</style>
