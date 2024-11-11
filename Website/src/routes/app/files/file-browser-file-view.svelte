<script lang="ts">
	import { useServerContext, type FileResource } from '$lib/client/client';
	import { type Snippet } from 'svelte';
	import Overlay from '../../overlay.svelte';
	import FileBrowserRefresh from './file-browser-refresh.svelte';
	import { useAppContext } from '$lib/client/contexts/app';

	const { file }: { file: FileResource } = $props();
	const { getFile, getFileContents, getFileSnapshots, scanFile } = useServerContext();

	const { isMobile, isDesktop } = useAppContext();

	async function load() {
		const fileContent = (await getFileContents(file.id, 0, 1))[0];
		const fileSnapshot = (await getFileSnapshots(file.id, fileContent.id, 0, 1))[0];
		const virusResult = await scanFile(file.id, fileContent.id, fileSnapshot.id);

		return {
			file,
			virusResult
		};
	}
</script>

<FileBrowserRefresh />

{#if $isMobile}
	<Overlay ondismiss={() => window.history.back()}>
		{#snippet children(windowButtons: Snippet)}
			<div class="file-view"></div>
		{/snippet}
	</Overlay>
{/if}

<div class="content">
	{#await load() then { file, virusResult }}
		<p>Virus Detected: {virusResult.viruses.join(', ')}</p>
	{/await}
</div>

<style lang="scss">
	@use '../../../global.scss' as *;

	div.content {
		// @include force-size(100dvw, 100dvh);
	}
</style>
