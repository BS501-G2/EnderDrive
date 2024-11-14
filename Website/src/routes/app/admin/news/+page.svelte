<script lang="ts">
	import { useAdminContext } from '$lib/client/contexts/admin';
	import { onMount } from 'svelte';
	import CreateNewsButton from './create-news-button.svelte';
	import AdminSidePanel from '../admin-side-panel.svelte';
	import FilterNewsButton from './filter-news-button.svelte';
	import CreateNewsDialog from './create-news-dialog.svelte';

	const { pushTitle, pushSidePanel } = useAdminContext();
	onMount(() => pushTitle('News'));

	let createDialog: [newsId?: number] | null = $state(null);
</script>

<FilterNewsButton />

<CreateNewsButton
	onopen={() => {
		createDialog = [];
	}}
/>

{#if createDialog != null}
	{@const [newsId] = createDialog}

	<CreateNewsDialog
		{newsId}
		ondismiss={() => {
			createDialog = null;
		}}
	/>
{/if}

<style lang="scss">
</style>
