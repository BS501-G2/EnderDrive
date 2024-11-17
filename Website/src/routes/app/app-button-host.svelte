<script
	lang="ts"
>
	import { useDashboardContext } from '$lib/client/contexts/dashboard';
	import Button from '$lib/client/ui/button.svelte';
	import {
		onMount,
		type Snippet
	} from 'svelte';
	import type { Readable } from 'svelte/store';
	import Overlay from '../overlay.svelte';
	import Icon, {
		type IconOptions
	} from '$lib/client/ui/icon.svelte';
	import { useAppContext } from '$lib/client/contexts/app';
	import Separator from '$lib/client/ui/separator.svelte';

	const {
		mobileAppButtons
	}: {
		mobileAppButtons: Readable<
			{
				id: number;
				snippet: Snippet<
					[
						ondismiss: () => void
					]
				>;
				show: boolean;
				icon: IconOptions;
				onclick: (
					event: MouseEvent & {
						currentTarget: EventTarget &
							HTMLButtonElement;
					}
				) => void;
			}[]
		>;
	} =
		$props();

	const {
		pushMobileTopRight
	} =
		useDashboardContext();
	const {
		isMobile
	} =
		useAppContext();

	onMount(
		() =>
			pushMobileTopRight(
				content
			)
	);

	let showMenu: boolean =
		$state(
			false
		);
	let element: HTMLButtonElement =
		$state(
			null as never
		);

	let x: number =
		$state(
			null as never
		);
	let y: number =
		$state(
			null as never
		);

	function onresize(
		element: HTMLButtonElement
	) {
		if (
			element
		) {
			const {
				height,
				top
			} =
				element.getBoundingClientRect();

			x =
				-1;
			y =
				top +
				height;
		}
	}

	$effect(
		() => {
			onresize(
				element
			);
		}
	);

	onMount(
		() =>
			isMobile.subscribe(
				(
					value
				) => {
					if (
						!value
					)
						showMenu = false;
				}
			)
	);
</script>

{#snippet content()}
	<div
		class="buttons"
	>
		{#snippet buttonForeground(
			view: Snippet
		)}
			<div
				class="button-foreground"
			>
				{@render view()}
			</div>
		{/snippet}

		{#each $mobileAppButtons as { id, show, icon, onclick } (id)}
			{#if show}
				<Button
					bind:buttonElement={element}
					hint="Actions"
					{onclick}
					foreground={buttonForeground}
				>
					<Icon
						{...icon}
						size="1em"
					/>
				</Button>
			{/if}
		{/each}

		{#if $mobileAppButtons.filter((entry) => !entry.show).length > 0}
			<Button
				bind:buttonElement={element}
				hint="Actions"
				onclick={() => {
					showMenu = true;
				}}
				foreground={buttonForeground}
			>
				<Icon
					icon="ellipsis-vertical"
					thickness="solid"
					size="1em"
				/>
			</Button>
		{/if}
	</div>
{/snippet}

{#if showMenu}
	<Overlay
		ondismiss={() =>
			(showMenu = false)}
		nodim
		{x}
		{y}
	>
		<div
			class="overlay"
		>
			{#each $mobileAppButtons as { id, snippet, show }, index (id)}
				{#if !show}
					{#if index !== 0}
						<Separator
							horizontal
						/>
					{/if}
					{@render snippet(
						() => {
							showMenu = false;
						}
					)}
				{/if}
			{/each}
		</div>
	</Overlay>
{/if}

<style
	lang="scss"
>
	div.buttons {
		flex-direction: row;
		align-items: center;
	}

	div.button-foreground {
		padding: 8px;
	}

	div.overlay {
		background-color: var(
			--color-9
		);

		border: solid
			1px
			var(
				--color-5
			);
		// box-shadow: 2px 2px 4px var(--color-5);
	}
</style>
