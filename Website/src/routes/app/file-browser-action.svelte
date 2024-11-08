<script lang="ts">
	import { useAppContext } from '$lib/client/contexts/app';
	import { useFileBrowserContext } from '$lib/client/contexts/file-browser';
	import Button from '$lib/client/ui/button.svelte';
	import Icon, { type IconOptions } from '$lib/client/ui/icon.svelte';
	import { onMount, type Snippet } from 'svelte';

	let {
		label,
		icon,
		onclick,

		buttonElement = $bindable(),
		type
	}: {
		label: string;
		icon: IconOptions;
		onclick: (
			event: MouseEvent & { currentTarget: EventTarget & HTMLButtonElement }
		) => void | Promise<void>;
		buttonElement?: HTMLButtonElement;
		type: 'left-main' | 'left' | 'right-main' | 'right';
	} = $props();

	const { isMobile } = useAppContext();
	const { pushAction } = useFileBrowserContext();

	onMount(() => pushAction(content, type, icon, label, onclick));
</script>

{#snippet content()}
	{#snippet foreground(view: Snippet)}
		<div class="action" class:mobile={$isMobile}>
			{@render view()}
		</div>
	{/snippet}

	<Button {onclick} {foreground} bind:buttonElement>
		{#if $isMobile}
			<Icon {...icon} size="1.2em" />
		{:else}
			<Icon {...icon} />
		{/if}

		<p>{label}</p>
	</Button>
{/snippet}

<style lang="scss">
	div.action {
		padding: 8px;
		gap: 8px;

		flex-direction: row;
		align-items: center;
		justify-content: center;

		line-height: 1em;
	}

	div.action.mobile {
		flex-direction: column;
	}
</style>
