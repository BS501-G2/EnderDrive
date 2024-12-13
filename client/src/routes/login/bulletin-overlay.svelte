<script lang="ts">
	import { goto } from '$app/navigation';
	import { getConnection } from '$lib/client/client';
	import type { Writable } from 'svelte/store';
	import BulletinIndicators from './bulletin-indicators.svelte';

	const { currentIndex, count }: { currentIndex: Writable<number>; count: number } = $props();
	const {
		serverFunctions: { getNews }
	} = getConnection();

	async function a() {
		const news = await getNews($currentIndex);

		if (news.link != null) {
			await goto(news.link);
		}
	}
</script>

<div class="overlay">
	<div class="top"></div>
	<div class="middle">
		<button onclick={a} aria-label="link"> </button>
	</div>
	<div class="bottom">
		<BulletinIndicators {currentIndex} {count} />
	</div>
</div>

<style lang="scss">
	div.overlay {
		position: relative;

		left: 0px;
		top: -100%;

		display: flex;
		flex-direction: column;

		min-height: 100%;
		max-height: 100%;

		> div.top {
			display: flex;
			flex-direction: row;

			min-height: 32px;
			max-height: 32px;

			justify-content: safe center;
		}

		> div.middle {
			flex-grow: 1;

			display: flex;
			flex-direction: column;

			justify-content: center;

			> button {
				flex-grow: 1;

				background-color: transparent;
				border: none;
				outline: none;
			}
		}

		> div.bottom {
			display: flex;
			flex-direction: row;

			background-color: var(--backgroundVariant);

			min-height: 32px;
			max-height: 32px;

			justify-content: safe center;
		}
	}
</style>
