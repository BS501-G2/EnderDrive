<script lang="ts">
	import type { FileResource } from '$lib/client/client';
	import { type Snippet } from 'svelte';
	import Overlay from '../../overlay.svelte';
	import FileBrowserRefresh from './file-browser-refresh.svelte';
	import { useAppContext } from '$lib/client/contexts/app';

	const { file }: { file: FileResource } = $props();

	const { isMobile, isDesktop } = useAppContext();
</script>

<FileBrowserRefresh />

{#if $isMobile}
	<Overlay ondismiss={() => window.history.back()}>
		{#snippet children(windowButtons: Snippet)}
			<div class="file-view">
					
			</div>
		{/snippet}
	</Overlay>
{/if}

<style lang="scss">
	@use '../../../global.scss' as *;

	div.content {
		@include force-size(100dvw, 100dvh);
	}
</style>
