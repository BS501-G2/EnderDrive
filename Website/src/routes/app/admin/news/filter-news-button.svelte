<script lang="ts">
	import { useAppContext } from '$lib/client/contexts/app';
	import AppButton from '../../app-button.svelte';
	import AdminSidePanel from '../admin-side-panel.svelte';

	const { isMobile, isDesktop } = useAppContext();
</script>

{#if $isDesktop}

	<AdminSidePanel name="Filter" icon={{ icon: 'filter', thickness: 'solid' }}>
		<p>There are currently no filters available.</p>
	</AdminSidePanel>
{/if}
