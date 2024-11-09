<script lang="ts">
	import { useFileBrowserContext, type CurrentFile } from '$lib/client/contexts/file-browser';
	import { createFileBrowserListContext } from '$lib/client/contexts/file-browser-list';
	import { onMount } from 'svelte';
	import FileBrowserFileListEntry from './file-browser-file-list-entry.svelte';
	import { persisted } from 'svelte-persisted-store';
	import FileBrowserAction from './file-browser-action.svelte';
	import FileBrowserCreateFolder from './file-browser-create-folder.svelte';
	import { useServerContext } from '$lib/client/client';

	const {
		current
	}: { current: CurrentFile & { type: 'folder' | 'shared' | 'starred' | 'trash' } } = $props();

	const { setFileListContext, refresh } = useFileBrowserContext();
	const { uploadFile, uploadBuffer, finishBuffer } = useServerContext();
	const { context } = createFileBrowserListContext();

	onMount(() => setFileListContext(context));

	let newFolder: boolean = $state(false);
	let uploadElement: HTMLInputElement & { type: 'file' } = $state(null as never);
	let uploadPromise: { resolve: (data: File[]) => void } | null = $state(null);
</script>

{#if current.type === 'folder'}
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

				for (const file of files) {
					const streamId = await uploadFile(current.file.id, file.name);
					const bufferSize = 1024 * 256;

					for (let index = 0; index < file.size; index += bufferSize) {
						const buffer = file.slice(index, index + bufferSize);

						await uploadBuffer(streamId, buffer);
					}

					await finishBuffer(streamId);
				}

				refresh()
			} finally {
				uploadPromise = null;
			}
		}}
	/>

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
			ondismiss={() => {
				newFolder = false;
			}}
		/>
	{/if}
{/if}

<div class="list-container">
	<div class="list">
		<div class="header"></div>

		{#each current.files as file}
			<FileBrowserFileListEntry {file} />
		{/each}
	</div>
</div>

<style lang="scss">
	div.list-container {
		flex-grow: 1;

		overflow: auto auto;
		min-height: 0;

		> div.list {
			> div.header {
			}
		}
	}
</style>
