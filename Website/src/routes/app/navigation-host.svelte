<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { useAppContext } from '$lib/client/contexts/app';
	import { type NavigationEntry } from '$lib/client/contexts/navigation';
	import { derived, type Readable } from 'svelte/store';
	import Navigation from './navigation.svelte';

	const { isMobile } = useAppContext()
	const { navigationEntries }: { navigationEntries: Readable<NavigationEntry[]> } = $props();
</script>

<div class="navigation" class:mobile={$isMobile}>
	{#each $navigationEntries as { id, snippet } (id)}
		{@render snippet()}
	{/each}
</div>


<Navigation
	label="Feed"
	onclick={async () => goto('/app/feed')}
	icon={(isActive) => ({ icon: 'newspaper', thickness: isActive ? 'solid' : 'regular' })}
	isActive={derived(page, ({ url: { pathname } }) => {
		if (pathname === '/app/feed') {
			return true;
		}

		return false;
	})}
/>

<Navigation
	label="Files"
	onclick={async () => goto('/app/files')}
	icon={(isActive) => ({ icon: 'folder-open', thickness: isActive ? 'solid' : 'regular' })}
	isActive={derived(page, ({ url: { pathname } }) => {
		if (pathname === '/app/files') {
			return true;
		}

		return false;
	})}
/>

<Navigation
	label="Shared"
	onclick={async () => goto('/app/shared')}
	icon={(isActive) => ({ icon: 'user', thickness: isActive ? 'solid' : 'regular' })}
	isActive={derived(page, ({ url: { pathname } }) => {
		if (pathname === '/app/shared') {
			return true;
		}

		return false;
	})}
/>

<Navigation
	label="Trash"
	onclick={async () => goto('/app/trash')}
	icon={(isActive) => ({ icon: 'trash-can', thickness: isActive ? 'solid' : 'regular' })}
	isActive={derived(page, ({ url: { pathname } }) => {
		if (pathname === '/app/trash') {
			return true;
		}

		return false;
	})}
/>
<style lang="scss">
	div.navigation {
		flex-grow: 1;

		justify-content: center;
	}

	div.mobile {
		flex-direction: row;
	}
</style>
