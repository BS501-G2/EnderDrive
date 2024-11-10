<script lang="ts">
	import FileBrowser from './file-browser.svelte';
	import { page } from '$app/stores';
	import { FileBrowserResolveType } from '$lib/client/contexts/file-browser';
	import { derived } from 'svelte/store';
	import { goto } from '$app/navigation';

	const resolve = derived(
		page,
		({ url: { searchParams } }): [file: FileBrowserResolveType.File, fileId: string | null] => {
			return [FileBrowserResolveType.File, searchParams.get('fileId')];
		}
	);
</script>

<FileBrowser
	{resolve}
	onFileId={(event, fileId) => {
		console.log(fileId);
		return goto('/app/files?fileId=' + fileId);
	}}
/>
