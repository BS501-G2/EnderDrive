<script lang="ts">
	import { useDashboardContext } from '$lib/client/contexts/dashboard';
	import Button from '$lib/client/ui/button.svelte';
	import Icon, { type IconOptions } from '$lib/client/ui/icon.svelte';
	import { onMount, type Snippet } from 'svelte';

	const {
		label,
		onclick,
		icon,
		show = false
	}: {
		label: string;
		onclick: (event: MouseEvent & { currentTarget: EventTarget & HTMLButtonElement }) => void;
		icon: IconOptions
		show?: boolean
	} = $props();
	const { pushMobileAppButton } = useDashboardContext();

	onMount(() => pushMobileAppButton(content, show, icon, onclick));
</script>

{#snippet content(ondismiss: () => void)}
	{#snippet buttonForeground(view: Snippet)}
		<div class="button-foreground">
			{@render view()}
		</div>
	{/snippet}

	<Button
		onclick={(event) => {
			const result = onclick(event);
			ondismiss();

			return result;
		}}
		foreground={buttonForeground}
	>
		<Icon {...icon} size="1em" />
		<p class="label">{label}</p>
	</Button>
{/snippet}

<style lang="scss">
	div.button-foreground {
		flex-direction: row;
		flex-grow: 1;

		padding: 16px;
		gap: 16px;
		line-height: 1em;

        text-align: start;
	}
</style>
