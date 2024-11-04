<script lang="ts">
	import { useLandingContext } from '$lib/client/contexts/landing';
	import { onMount, type Snippet } from 'svelte';

	const {
		name,
		children,
		contain = false,
		hideButton = false,
		hideHeader = false
	}: {
		name: string;
		children: Snippet;
		contain?: boolean;
		hideButton?: boolean;
		hideHeader?: boolean;
	} = $props();
	const { pushLandingEntry } = useLandingContext();

	onMount(() => pushLandingEntry(name, container, !hideButton));
</script>

{#snippet content()}
	<div class="content">
		{#if !hideHeader}
			<h2 class="content-header">{name}</h2>
		{/if}

		<div class="content-body">
			{@render children()}
		</div>
	</div>
{/snippet}

{#snippet container()}
	{#if contain}
		<div class="container">
			<div class="inner-container">
				{@render content()}
			</div>
		</div>
	{:else}
		{@render content()}
	{/if}
{/snippet}

<style lang="scss">
	@use '../../global.scss' as *;

	div.container {
		align-items: center;

		> div.inner-container {
			@include force-size(min(1280px, 100%), &);

			padding: 0px 16px;
			box-sizing: border-box;
		}
	}

	div.content {
		gap: 32px;

		> h2.content-header {
			font-size: 2em;
		}

		> div.content-body {
			text-align: justify;
		}
	}
</style>
