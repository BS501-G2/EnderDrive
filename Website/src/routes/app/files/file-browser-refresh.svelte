<script lang="ts">
	import { useAppContext } from '$lib/client/contexts/app';
	import { useFileBrowserContext } from '$lib/client/contexts/file-browser';
	import FileBrowserAction from './file-browser-action.svelte';

	const { refresh } = useFileBrowserContext();
	const { isDesktop, isMobile } = useAppContext();
</script>

{#if $isDesktop}
	<FileBrowserAction
		label="Refresh"
		icon={{ icon: 'refresh', thickness: 'solid' }}
		onclick={refresh}
		type="right"
	/>
{:else if $isMobile}
	<FileBrowserAction
		label="Refresh"
		icon={{ icon: 'refresh', thickness: 'solid' }}
		onclick={refresh}
		type="right-main"
	/>
{/if}
