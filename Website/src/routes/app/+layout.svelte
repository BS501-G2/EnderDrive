<script lang="ts" module>
</script>

<script lang="ts">
	import { useAppContext } from '$lib/client/contexts/app';
	import {
		createDashboardContext,
		type TopbarContentPosition
	} from '$lib/client/contexts/dashboard';
	import Favicon from '$lib/client/ui/favicon.svelte';

	import { onMount, type Snippet } from 'svelte';
	import { writable, type Writable } from 'svelte/store';

	const {
		mainTop,
		mainContent,
		side,
		context: { pushTopContent, pushSideContent }
	} = createDashboardContext();
	const { isLimitedDesktop } = useAppContext();

	const { children }: { children: Snippet } = $props();

	onMount(() => pushSideContent(logo));
</script>

{#snippet logo()}
	<div class="site-logo">
		<Favicon size={32} />

		<p>EnderDrive</p>
	</div>
{/snippet}

<div class="dashboard">
	<div class="side" class:limited={$isLimitedDesktop}>
		{#each $side as sideEntry}
			{@render sideEntry()}
		{/each}
	</div>

	<div class="main">
		{@render children()}
	</div>
</div>

<style lang="scss">
	@use '../global.scss' as *;

	div.dashboard {
		flex-direction: row;
		flex-grow: 1;

		div.side {
			@include force-size(172px, &);

			background-color: var(--color-1);
		}

		div.side.limited {
			@include force-size(72px, &);
		}
	}

	div.site-logo {
		flex-direction: row;

		align-items: center;
		justify-content: center;


		margin: 16px 0;
		font-size: 1.5rem;
		font-weight: lighter;

		color: var(--color-);
	}
</style>
