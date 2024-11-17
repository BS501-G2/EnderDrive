<script
	lang="ts"
>
	import { useAppContext } from '$lib/client/contexts/app';
	import { type FileBrowserAction } from '$lib/client/contexts/file-browser';
	import type { Readable } from 'svelte/store';
	import FileBrowserActionHostDesktop from './file-browser-action-host-desktop.svelte';
	import FileBrowserActionHostMobile from './file-browser-action-host-mobile.svelte';

	const {
		actions
	}: {
		actions: Readable<
			FileBrowserAction[]
		>;
	} =
		$props();

	const {
		isMobile,
		isDesktop
	} =
		useAppContext();
</script>

{#if $actions.length != 0}
	{#if $isDesktop}
		<FileBrowserActionHostDesktop
			{actions}
		/>
	{/if}

	{#if $isMobile}
		<FileBrowserActionHostMobile
			{actions}
		/>
	{/if}
{/if}
