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
	const {
		createFile,
		writeStream,
		closeStream,
		getFileMime,
		getFileStars,
		setFileStar,
		getFileStar
	} = useServerContext();
	const { context, selectedFileIds } = createFileBrowserListContext();
	const { isMobile } = useAppContext();

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
		{#await Promise.all($selectedFileIds.map(async (fileId) => {
				return await getFileStar(fileId);
			})) then starred}
			<FileBrowserAction
				type="left"
				icon={starred.some((file) => file)
					? { icon: 'star', thickness: 'solid' }
					: { icon: 'star', thickness: 'regular' }}
				label="Starred"
				onclick={async () => {
					for (const fileId of $selectedFileIds) {
						if (starred.some((file) => file)) {
							await setFileStar(fileId, false);
						} else {
							await setFileStar(fileId, true);
						}
					}

					refresh();
				}}
			/>
		{/await}
		<FileBrowserAction
			type="left"
			icon={{ icon: 'trash-can', thickness: 'regular' }}
			label="Delete"
			onclick={() => {}}
		/>

		<FileBrowserAction
			type="left"
			icon={{ icon: 'pencil', thickness: 'solid' }}
			label="Rename"
			onclick={() => {}}
		/>

		<FileBrowserAction
			type="left"
			icon={{ icon: 'download', thickness: 'solid' }}
			label="Download"
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
		<div class="header"></div>
		<div class="list" class:mobile={$isMobile}>
			{#each current.files as file}
				{#if file.type === 'folder'}
					<FileBrowserFileListEntry {file} />
				{:else}
					{#if selectMode}
						{#if selectMode.allowedFileMimeTypes.length !== 0}
							{#await getFileMime(file.file.id) then mime}
								{#if selectMode.allowedFileMimeTypes.some( (mimeType) => (mimeType instanceof RegExp ? mimeType.test(mime) : mimeType === mime) )}
									<FileBrowserFileListEntry {file} />
								{/if}
							{/await}
						{:else}
							<FileBrowserFileListEntry {file} />
						{/if}
					{:else}
						<FileBrowserFileListEntry {file} />
					{/if}
				{/if}
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

			overflow: auto;
			min-height: 0;
			min-width: 0;

			> div.list.mobile {
				// gap: 8px;
			}
		}
	}
</style>
