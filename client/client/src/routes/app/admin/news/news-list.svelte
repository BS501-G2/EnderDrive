<script lang="ts">
	import { getConnection } from '$lib/client/client';
	import type { News } from '@rizzzi/enderdrive-lib/server';
	import NewsEntry from './news-entry.svelte';
	import { LoadingSpinner } from '@rizzzi/svelte-commons';
	import { range } from '$lib/range';

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
