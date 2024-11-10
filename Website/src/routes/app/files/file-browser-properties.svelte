<script lang="ts">
	import type { FileResource } from '$lib/client/client';
	import { useFileBrowserContext, type FileEntry } from '$lib/client/contexts/file-browser';
	import type { FileBrowserListContext } from '$lib/client/contexts/file-browser-list';
	import type { Readable } from 'svelte/store';
	import { fly } from 'svelte/transition';

	const { selectedFileIds }: { selectedFileIds: string[] } = $props();
</script>

<div class="properties" transition:fly={{ x: 16, duration: 150 }}>
	<h2>asdasd</h2>
</div>

<style lang="scss">
	@use '../../../global.scss' as *;

	div.properties {
		@include force-size(256px, &);
	}
</style>
