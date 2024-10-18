<script lang="ts" module>
	export type BannerClass = 'info' | 'warning' | 'error';
</script>

<script lang="ts">
	import type { Snippet } from 'svelte';

	const { bannerClass = 'info', children }: { bannerClass?: BannerClass; children: Snippet } =
		$props();
</script>

<div class="banner {bannerClass}">
	{@render children()}
</div>

<style lang="scss">
	div.banner {
		padding: 16px;
		box-sizing: border-box;
		border-radius: 8px;
	}

	div.banner.error {
		border: solid 1px var(--error);
		background-color: var(--errorBackground);
		color: var(--onError);
	}

	div.banner.warning {
		border: solid 1px var(--warning);
		background-color: var(--warningBackground);
		color: var(--onWarning);
	}

	div.banner.info {
		border: solid 1px var(--info);
		background-color: var(--infoBackground);
		color: var(--onInfo);
	}
</style>
