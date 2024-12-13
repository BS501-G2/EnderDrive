<script lang="ts">
	import { getConnection } from '$lib/client/client';
	import BulletinNews from './bulletin-news.svelte';
	import { type Writable } from 'svelte/store';
	import { tweened } from 'svelte/motion';
	import { onMount } from 'svelte';
	import { range } from '$lib/range';
	import { quadInOut } from 'svelte/easing';
	import LoadingSpinner from '$lib/widgets/loading-spinner.svelte';

	const { currentIndex, count }: { currentIndex: Writable<number>; count: number } = $props();
	const {
		serverFunctions: { getNewsCount, getNews }
	} = getConnection();

	const rawScroll: Writable<number> = tweened(0, {
		easing: quadInOut,
		duration: 500
	});

	let carouselElement: HTMLElement = $state(null as never);

	onMount(() =>
		currentIndex.subscribe((value) => rawScroll.set(value * carouselElement.clientWidth))
	);

	onMount(() =>
		rawScroll.subscribe((value) => {
			carouselElement.scrollLeft = value;
		})
	);

	onMount(() => {
		const interval = setInterval(() => {
			$currentIndex = ($currentIndex + 1) % count;
		}, 5000);

		return () => clearInterval(interval);
	});
</script>

<div class="carousel" bind:this={carouselElement}>
	{#each range(count) as index}
		{#await getNews(index)}
			<LoadingSpinner size="1em" />
		{:then news}
			<BulletinNews {news} />
		{/await}
	{/each}
</div>

<style lang="scss">
	div.carousel {
		flex-grow: 1;

		display: flex;
		flex-direction: row;

		overflow: auto hidden;

		min-height: 100%;
		max-height: 100%;
	}

	div.carousel::-webkit-scrollbar {
		display: none;
	}

	div.carousel {
		-ms-overflow-style: none;
		scrollbar-width: none;
	}
</style>
