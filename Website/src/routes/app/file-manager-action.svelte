<script lang="ts">
	import { useFileBrowserContext } from '$lib/client/contexts/file-browser';
	import Button from '$lib/client/ui/button.svelte';
	import Icon, { type IconOptions } from '$lib/client/ui/icon.svelte';
	import { onMount, type Snippet } from 'svelte';

	const {
		label,
		icon,
		onclick
	}: {
		label: string;
		icon: IconOptions;
		onclick: (
			event: MouseEvent & { currentTarget: EventTarget & HTMLButtonElement }
		) => void | Promise<void>;
	} = $props();
	const { pushAction } = useFileBrowserContext();

	onMount(() => pushAction(content));
</script>

{#snippet content()}
	{#snippet foreground(view: Snippet)}
		<div class="action">
			{@render view()}
		</div>
	{/snippet}

	<Button {onclick} {foreground}>
		<Icon {...icon} />

		<p>{label}</p>
	</Button>
{/snippet}

<style lang="scss">
	div.action {
		padding: 8px;

		flex-direction: row;
		align-items: center;
	}
</style>
