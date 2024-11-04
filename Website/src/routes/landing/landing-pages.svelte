<script lang="ts">
	import { derived, get, writable, type Readable, type Writable } from 'svelte/store';
	import LandingPageButton from './landing-page-button.svelte';
	import type { LandingPageEntry } from '$lib/client/contexts/landing';
	import { tweened } from 'svelte/motion';
	import { cubicInOut } from 'svelte/easing';
	import { onMount } from 'svelte';

	const {
		pages,
		currentPage
	}: {
		pages: Readable<LandingPageEntry[]>;
		currentPage: Readable<number>;
	} = $props();

	const widths: Writable<[number, Readable<number>, Readable<number>][]> = writable([]);
	const flattenedCurrentPage = derived(currentPage, (value) => Math.round(value));

	const offset = tweened($currentPage, {
		duration: 250,
		easing: cubicInOut
	});

	const width = tweened(0, {
		duration: 250,
		easing: cubicInOut
	});

	const a = derived(
		[flattenedCurrentPage, pages, widths],
		([currentPage, pageButtons, widths]): [barLength: number, barPosition: number] => {
			const page = pageButtons[currentPage];

			if (page != null) {
				const sizeIndex = widths.findIndex((value) => value[0] === page.id);

				if (sizeIndex != -1) {
					return [get(widths[sizeIndex][1]), get(widths[sizeIndex][2])];
				}
			}

			return [0, 2];
		}
	);

	onMount(() =>
		a.subscribe((result) => {
			$offset = result[0];
			$width = result[1];
		})
	);
</script>

<div class="entries-container">
	<div class="entries">
		{#each $pages as { id, name }, index (id)}
			{#if index !== 0}
				<div class="divider"></div>
			{/if}

			<LandingPageButton
				{id}
				{name}
				{widths}
				isActive={index === $flattenedCurrentPage}
				onclick={() => {
					$currentPage = index;
				}}
			/>
		{/each}
	</div>

	<div class="bar-container">
		<div
			class="bar"
			style:min-width="{$width}px"
			style:max-width="{$width}px"
			style:margin-left="{$offset}px"
		></div>
	</div>
</div>

<style lang="scss">
	@use '../../global.scss' as *;

	div.entries-container {
		flex-direction: column;
		flex-grow: 1;

		margin: 8px 0px;

		> div.entries {
			flex-direction: row;
			flex-grow: 1;

			gap: 8px;
		}

		> div.bar-container {
			flex-direction: row;

			@include force-size(&, 2px);

			> div.bar {
				background-color: var(--color-5);
			}
		}
	}

	div.divider {
		@include force-size(1px, &);

		background-color: var(--color-5);
		margin: 16px 0;
	}
</style>
