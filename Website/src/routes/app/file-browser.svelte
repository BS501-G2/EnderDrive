<script lang="ts">
	import {
		createFileBrowserContext,
		useFileBrowserContext,
		type FileBrowserOptions
	} from '$lib/client/contexts/file-browser';
	import Separator from '$lib/client/ui/separator.svelte';
	import FileBrowserPath from './file-browser-path.svelte';
	import FileManagerActionHost from './file-browser-action-host.svelte';
	import FileBrowserProperties from './file-browser-properties.svelte';
	import { useAppContext } from '$lib/client/contexts/app';
	import FileBrowserResolver from './file-browser-resolver.svelte';

	const { resolve, onFileId }: FileBrowserOptions = $props();
	const { actions, top, current, middle, bottom } = createFileBrowserContext(onFileId);
	const { showDetails } = useFileBrowserContext();
	const { isMobile, isDesktop } = useAppContext();
</script>

<div class="file-browser">
	<div class="left" >
		{#if $top.length}
			<div class="top">
				{#each $top as { id, snippet }, index (id)}
					{#if index > 0}
						<Separator horizontal />
					{/if}

					{@render snippet()}
				{/each}
			</div>

			<Separator horizontal />
		{/if}

		<div class="middle">
			{#if resolve}
				<FileBrowserResolver {resolve} {current} />
			{/if}

			{#each $middle as { id, snippet } (id)}
				{@render snippet()}
			{/each}
		</div>

		{#if $bottom.length}
			<Separator horizontal />

			<div class="bottom">
				{#each $bottom as { id, snippet }, index (id)}
					{#if index > 0}
						<Separator horizontal />
					{/if}

					{@render snippet()}
				{/each}
			</div>
		{/if}
	</div>

	{#if $isMobile && $showDetails}
		<div class="right">
			<FileBrowserProperties />
		</div>
	{/if}
</div>

{#if $current.type === 'file' || $current.type === 'folder' || $current.type === 'loading'}
	<FileBrowserPath current={$current} />
{/if}
<FileManagerActionHost {actions} />

<style lang="scss">
	@use '../../global.scss' as *;

	div.file-browser {
		flex-grow: 1;

		min-width: 0;
		min-height: 0;

		> div.left {
			flex-grow: 1;

			> div.top {
				flex-direction: column;
			}

			> div.middle {
				flex-grow: 1;
				flex-direction: row;

				overflow: hidden auto;

				min-height: 0;

				> div.loading {
					align-self: center;
				}
			}

			> div.bottom {
				flex-direction: row;
			}
		}

		> div.right {
		}
	}
</style>
