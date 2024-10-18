<script lang="ts">
	import { getConnection } from '$lib/client/client';
	import NewsEntry from './news-entry.svelte';
	import { range } from '$lib/range';
	import LoadingSpinner from '$lib/widgets/loading-spinner.svelte';

	const {
		serverFunctions: { getNewsCount, getNews }
	} = getConnection();

	const { refresh }: { refresh: () => void } = $props();
</script>

<div class="news-list">
	{#await getNewsCount()}
		<LoadingSpinner size="1em" />
	{:then count}
		{#each range(count) as index}
			<NewsEntry {index} {refresh} />
		{/each}
	{/await}
</div>

<style lang="scss">
	div.news-list {
		display: flex;
		flex-direction: column;
	}
</style>
