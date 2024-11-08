<script lang="ts">
	import { useFileBrowserContext, type CurrentFile } from '$lib/client/contexts/file-browser';
	import { createFileBrowserListContext } from '$lib/client/contexts/file-browser-list';
	import { onMount } from 'svelte';
	import FileBrowserCreate from './file-browser-create.svelte';
	import FileBrowserFileListEntry from './file-browser-file-list-entry.svelte';
	import { persisted } from 'svelte-persisted-store';
	import FileBrowserAction from './file-browser-action.svelte';

	const {
		current
	}: { current: CurrentFile & { type: 'folder' | 'shared' | 'starred' | 'trash' } } = $props();

	const { setFileListContext } = useFileBrowserContext();
	const { context } = createFileBrowserListContext();

	onMount(() => setFileListContext(context));

	interface Resize {
		preview: number;
	}

	const a = persisted<Resize>('a', {
		preview: 32
	});

	let createButton: HTMLButtonElement = $state(null as never);
	let create: boolean = $state(true);
</script>

{#if current.type === 'folder'}
	<FileBrowserAction
		bind:buttonElement={createButton}
		type="left-main"
		icon={{ icon: 'plus', thickness: 'solid' }}
		label="Create"
		onclick={() => {
			create = true;
		}}
	/>

	{#if create}
		<FileBrowserCreate
			ondismiss={() => {
				create = false;
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

		> div.list {
			> div.header {
			}
		}
	}
</style>
