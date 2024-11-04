<script lang="ts">
	import type { LandingPageEntry } from '$lib/client/contexts/landing';
	import Icon from '$lib/client/ui/icon.svelte';
	import Overlay from '../overlay.svelte';
	import type { Readable, Writable } from 'svelte/store';
	import { fly } from 'svelte/transition';

	const {
		pages,
		currentPage
	}: { pages: Writable<LandingPageEntry[]>; currentPage: Readable<number> } = $props();

	let expanded: boolean = $state(false);
</script>

<button
	class="open"
	onclick={() => {
		expanded = true;
	}}
>
	<Icon icon="bars" thickness="solid" />
</button>

{#if expanded}
	<Overlay>
		<div class="test" transition:fly={{ duration: 250, y: -16 }}>
			<button
				class="dismiss begin"
				onclick={() => {
					expanded = false;
				}}
				aria-label="Close Menu"
			></button>

			<div class="list">
				{#each $pages as { id, name, content }, index (id)}
					<button
						onclick={() => {
							$currentPage = index;
							expanded = false;
						}}
						class:active={$currentPage === index}
					>
						{name}
					</button>

					<div class="divider"></div>
				{/each}
			</div>
			<button
				class="dismiss"
				onclick={() => {
					expanded = false;
				}}
				aria-label="Close Menu"
			></button>
		</div>
	</Overlay>
{/if}

<style lang="scss">
	@use '../../global.scss' as *;

	button.open {
		margin: 8px 0px;
		padding: 0px 16px;

		border-radius: 8px;
		border: none;

		color: var(--color-5);
		font-weight: lighter;

		font-size: 1.5em;
	}

	div.test {
		@include force-size(100dvw, 100dvh);

		color: var(--color-5);

		> div.list {
			padding: 8px 16px;

			> div.divider {
				@include force-size(&, 1px);

				background-color: var(--color-5);
			}

			> button {
				text-align: start;
				padding: 16px;

				border: none;
			}

			> button.active {
				font-weight: bolder;
			}
		}

		> button.dismiss {
			flex-grow: 1;

			border: none;
		}

		> button.dismiss.begin {
			@include force-size(&, 64px);
		}
	}
</style>
