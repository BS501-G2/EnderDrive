<script lang="ts">
	import FileBrowser from '../file-browser.svelte';
	import { page } from '$app/stores';
	import { FileBrowserResolveType } from '$lib/client/contexts/file-browser';
	import { derived } from 'svelte/store';

	const fileId = derived(page, ({ url: { searchParams } }): string | null =>
		searchParams.get('fileId')
	);
</script>

<FileBrowser resolve={[FileBrowserResolveType.File, $fileId]} onFileId={async () => {
	
}} />
