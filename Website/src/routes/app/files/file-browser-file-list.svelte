<script lang="ts">
	import { useFileBrowserContext, type CurrentFile } from '$lib/client/contexts/file-browser';
	import { createFileBrowserListContext } from '$lib/client/contexts/file-browser-list';
	import { onMount } from 'svelte';
	import FileBrowserFileListEntry from './file-browser-file-list-entry.svelte';
	import { persisted } from 'svelte-persisted-store';
	import FileBrowserAction from './file-browser-action.svelte';
	import FileBrowserCreateFolder from './file-browser-create-folder.svelte';
	import { useServerContext } from '$lib/client/client';
	import FileBrowserRefresh from './file-browser-refresh.svelte';
	import Title from '../title.svelte';
	import { useAppContext } from '$lib/client/contexts/app';

	const {
		current
	}: { current: CurrentFile & { type: 'folder' | 'shared' | 'starred' | 'trash' } } = $props();

	const { setFileListContext, refresh, selectMode } = useFileBrowserContext();
	const { createFile, writeStream, closeStream } = useServerContext();
	const { context, selectedFileIds } = createFileBrowserListContext();
	const { isMobile } = useAppContext()

	onMount(() => setFileListContext(context));

	let newFolder: boolean = $state(false);
	let uploadElement: HTMLInputElement & { type: 'file' } = $state(null as never);
	let uploadPromise: { resolve: (data: File[]) => void } | null = $state(null);
</script>

<FileBrowserRefresh />

{#if $selectedFileIds.length > 0}
	<Title title="{$selectedFileIds.length} Selected" />
{/if}

{#if current.type === 'folder' && selectMode == null}

	<FileBrowserAction
		type="left-main"
		icon={{ icon: 'plus', thickness: 'solid' }}
		label="New Folder"
		onclick={() => {
			newFolder = true;
		}}
	/>

	<FileBrowserAction
		type="left-main"
		icon={{ icon: 'plus', thickness: 'solid' }}
		label="Upload"
		onclick={async () => {
			uploadElement.click();

			let files: File[];

			try {
				files = await new Promise((resolve) => {
					uploadPromise = { resolve };
				});

				if (files.length === 0) {
					return;
				}

				for (const file of files) {
					const streamId = await createFile(current.file.id, file.name);
					const bufferSize = 1024 * 256;

					for (let index = 0; index < file.size; index += bufferSize) {
						const buffer = file.slice(index, index + bufferSize);

						await writeStream(streamId, buffer);
					}

					await closeStream(streamId);
				}

				refresh();
			} finally {
				uploadPromise = null;
			}
		}}
	/>

	{#if $selectedFileIds.length > 0}
		<FileBrowserAction
			type="left"
			icon={{ icon: 'trash', thickness: 'solid' }}
			label="Delete"
			onclick={() => {}}
		/>
	{/if}

	<input
		type="file"
		hidden
		multiple
		bind:this={uploadElement as never}
		onchange={({ currentTarget }) => {
			const files = Array.from(currentTarget.files ?? []);

			if (files.length === 0) {
				return;
			}

			uploadPromise?.resolve(files);
		}}
		oncancel={() => {
			uploadPromise?.resolve([]);
		}}
	/>

	{#if newFolder}
		<FileBrowserCreateFolder
			parentFolder={current.file}
			ondismiss={() => {
				newFolder = false;
			}}
		/>
	{/if}
{/if}

<div class="container">
	<div class="list-header"></div>

	<div class="list-container">
			<div class="header">

			</div>
		<div class="list" class:mobile={$isMobile}>

			{#each current.files as file}
				<FileBrowserFileListEntry {file} />
			{/each}
		</div>
	</div>
</div>

<style lang="scss">
	div.container {
		flex-grow: 1;

		min-height: 0;
		min-width: 0;

		div.list-container {
			flex-grow: 1;

			overflow: auto auto;
			min-height: 0;
			min-width: 0;

			> div.list.mobile {
				// gap: 8px;
			}
		}
	}
</style>
