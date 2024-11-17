<script
	lang="ts"
>
	import { useAppContext } from '$lib/client/contexts/app';
	import {
		useLandingContext,
		type LandingPageButton
	} from '$lib/client/contexts/landing';
	import Button from '$lib/client/ui/button.svelte';
	import Icon from '$lib/client/ui/icon.svelte';
	import { type Snippet } from 'svelte';
	import {
		derived,
		type Readable
	} from 'svelte/store';

	const {
		buttons,
		opacity
	}: {
		buttons: Readable<
			LandingPageButton[]
		>;
		opacity: Readable<number>;
	} =
		$props();

	const {
		isDesktop
	} =
		useAppContext();
	const {
		openLogin,
		closeLogin
	} =
		useLandingContext();
</script>

{#snippet action(
	{
		id,
		icon,
		content,
		isSecondary,
		onclick
	}: LandingPageButton,
	index: number
)}
	{#snippet container(
		view: Snippet
	)}
		<div
			class="action"
			class:secondary={isSecondary}
		>
			{@render view()}
		</div>
	{/snippet}

	<Button
		{onclick}
		background={container}
	>
		<div
			class="action-inner"
		>
			<Icon
				{...icon}
			/>

			{#if $isDesktop}
				{@render content()}
			{/if}
		</div>
	</Button>

	<!-- <button class="action" class:secondary={isSecondary} {onclick}>
		<Icon {...icon} />

		{#if $isDesktop}
			{@render content()}
		{/if}
	</button> -->
{/snippet}

<div
	class="actions"
>
	{#each $buttons as entry, index}
		{@render action(
			entry,
			index
		)}
	{/each}

	{@render action(
		{
			id: Date.now(),
			icon: {
				icon: 'key',
				thickness:
					'solid'
			},
			content:
				login,
			isSecondary: false,
			onclick:
				() => {
					openLogin();
				}
		},
		0
	)}

	{@render action(
		{
			id: Date.now(),
			icon: {
				icon: 'download',
				thickness:
					'solid'
			},
			content:
				download,
			isSecondary: true,
			onclick:
				() => {}
		},
		0
	)}
</div>

{#snippet download()}
	Download
{/snippet}

{#snippet login()}
	Login
{/snippet}

<style
	lang="scss"
>
	@use '../../global.scss'
		as *;

	div.actions {
		flex-direction: row;
		align-items: center;

		gap: 16px;
	}

	div.action {
		border: none;
		cursor: pointer;

		background-color: var(
			--color-1
		);

		-webkit-app-region: no-drag;
	}

	div.action-inner {
		flex-direction: row;
		align-items: center;

		gap: 8px;
		line-height: 1em;
		padding: 8px;
	}

	div.action.secondary {
		background-color: transparent;
	}
</style>
