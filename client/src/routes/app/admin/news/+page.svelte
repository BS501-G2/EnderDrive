<script lang="ts">
	import { getContext, onMount } from 'svelte';
	import { type AdminContext, AdminContextName } from '../+layout.svelte';
	import NewList from './news-list.svelte';
	import TopContent from './top-content.svelte';
	import CreateDialog from './create-dialog.svelte';

	const { pushTopContent, setMainContent } = getContext<AdminContext>(AdminContextName);

	onMount(() => pushTopContent(topContent));
	onMount(() => setMainContent(main));

	let create: boolean = $state(false);
	let refreshKey: number = $state(0);
</script>

{#snippet topContent()}
	<TopContent
		create={() => {
			create = true;
		}}
	/>
{/snippet}

{#snippet main()}
	{#key refreshKey}
		<NewList
			refresh={() => {
				refreshKey++;
			}}
		/>
	{/key}
{/snippet}

{#if create}
	<CreateDialog
		dismiss={() => {
			create = false;
		}}
		refresh={() => {
			refreshKey++;
		}}
	/>
{/if}

<style lang="scss">
	
</style>
