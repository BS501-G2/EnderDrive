<script lang="ts">
	import type { FileResource } from '$lib/client/client';
	import FileBrowserRefresh from './file-browser-refresh.svelte';

	const { file }: { file: FileResource } = $props();
</script>

<FileBrowserRefresh />
