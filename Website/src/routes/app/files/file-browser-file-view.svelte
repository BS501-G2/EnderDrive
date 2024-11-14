<script lang="ts">
	import { useServerContext, type FileResource } from '$lib/client/client';
	import { type Snippet } from 'svelte';
	import Overlay from '../../overlay.svelte';
	import FileBrowserRefresh from './file-browser-refresh.svelte';
	import { useAppContext } from '$lib/client/contexts/app';
	import FileBrowserFileContent from './file-browser-file-content.svelte';

	const { file }: { file: FileResource } = $props();
	const { getFile, getFileContents, getFileSnapshots, scanFile } = useServerContext();

	const { isMobile, isDesktop } = useAppContext();

</script>

<FileBrowserRefresh />

{#if $isMobile}
	<Overlay ondismiss={() => window.history.back()} x={0} y={0}>
		{#snippet children(windowButtons: Snippet)}
			<div class="overlay">
				<div class="header">
					<div class="title">
						<p>{file.name}</p>
					</div>

					{@render windowButtons()}
				</div>

				<div class="main">
					<FileBrowserFileContent fileId={file.id} />
				</div>
			</div>
		{/snippet}
	</Overlay>
{/if}

{#if $isDesktop}
	<div class="content">
		<FileBrowserFileContent fileId={file.id} />
	</div>
{/if}

<style lang="scss">
	@use '../../../global.scss' as *;

	div.content {
		// @include force-size(100dvw, 100dvh);
	}

	div.overlay {
		@include force-size(100dvw, 100dvh);

		background-color: var(--color-10);
		color: var(--color-5);

		> div.header {
			flex-direction: row;
			align-items: center;

			min-width: 0;

			> div.title {
				flex-direction: row;

				margin: 8px;
				font-weight: bolder;
				flex-grow: 1;
				min-width: 0;

				> p {
					overflow: hidden;

					text-wrap: nowrap;
					text-overflow: ellipsis;
					font-size: 1.2em;
					min-width: 0;
				}
			}
		}
	}
</style>
