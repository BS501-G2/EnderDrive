<script lang="ts">
	import { getConnection } from '$lib/client/client';
	import Icon from '$lib/ui/icon.svelte';
	import Button from '$lib/widgets/button.svelte';
	import type { News } from '@rizzzi/enderdrive-lib/server';
	import { onMount } from 'svelte';

	const { refresh, index }: { refresh: () => void; index: number } = $props();
	const {
		serverFunctions: { deleteNews, getNews }
	} = getConnection();

	let news: News | null = $state(null);

	onMount(() =>
		getNews(index).then((result) => {
			news = result;
		})
	);
</script>

<div class="news">
	<div class="banner">
		{#if news != null}
			<img src={URL.createObjectURL(new Blob([news.banner]))} alt="banner" />
		{/if}
	</div>
	<div class="title">
		<p><b>{news != null ? (news.title || '<no title>') : 'Loading...'}</b></p>
	</div>
	<div class="actions">
		<Button
			onClick={async (event) => {
				await deleteNews(index);
				refresh();
			}}
		>
			<div class="action">
				<Icon icon="trash" thickness="solid" />
				<p>Delete</p>
			</div>
		</Button>
	</div>
</div>

<style lang="scss">
	div.news {
		display: flex;
		flex-direction: row;

		gap: 16px;

		min-height: 72px;
		max-height: 72px;

		> div.banner {
			display: flex;
			flex-direction: column;

			justify-content: center;

			min-width: 128px;
			max-width: 128px;

			> img {
				min-width: 100%;
				max-width: 100%;
				max-width: 100%;
				max-height: 100%;

				object-fit: contain;
			}
		}

		> div.title {
			flex-grow: 1;

			display: flex;
			flex-direction: column;

			justify-content: center;
		}

		> div.actions {
			display: flex;
			flex-direction: row;
			align-items: center;

			flex-shrink: 0;

			div.action {
				display: flex;
				flex-direction: row;

				align-items: center;

				gap: 8px;
				padding: 4px;

				line-height: 1em;
			}
		}
	}
</style>
