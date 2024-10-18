<script lang="ts">
	import type { News } from '@rizzzi/enderdrive-lib/server';

	const { news }: { news: News } = $props();
</script>

<div class="news">
	<img src={URL.createObjectURL(new Blob([news.banner]))} alt="banner" />
</div>

<style lang="scss">
	div.news {
		flex-grow: 1;

		display: flex;
		flex-direction: column;

		justify-content: center;

		min-width: 100%;
		max-width: 100%;

		> img {
			min-width: 100%;
			max-width: 100%;
			min-height: 100%;
			max-height: 100%;

			object-fit: contain;
		}
	}
</style>
