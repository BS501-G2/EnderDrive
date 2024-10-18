<script lang="ts">
	import { writable, type Writable } from 'svelte/store';
	import BulletinCarousel from './bulletin-carousel.svelte';
	import BulletinOverlay from './bulletin-overlay.svelte';
	import { getConnection } from '$lib/client/client';
	import LoadingSpinner from '$lib/widgets/loading-spinner.svelte';

	const currentIndex: Writable<number> = writable(0);

	const {
		serverFunctions: { getNewsCount }
	} = getConnection();
</script>

<div class="banner">
	{#await getNewsCount()}
		<LoadingSpinner size="4em" />
	{:then count}
		<BulletinCarousel {currentIndex} {count} />
		<BulletinOverlay {currentIndex} {count} />
	{/await}
</div>

<style lang="scss">
	div.banner {
		background-color: var(--backgroundVariant);
		color: var(--onBackgroundVariant);

		padding: 16px;
		border-radius: 16px;
		box-sizing: border-box;

		flex-grow: 1;
	}
</style>
