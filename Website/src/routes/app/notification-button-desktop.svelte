<script lang="ts">
	import { useAppContext } from '$lib/client/contexts/app';
	import { useDashboardContext } from '$lib/client/contexts/dashboard';
	import Button from '$lib/client/ui/button.svelte';
	import Icon from '$lib/client/ui/icon.svelte';
	import { onMount, type Snippet } from 'svelte';

	const { pushDesktopTopRight, showNotifications } = useDashboardContext();

	onMount(() => pushDesktopTopRight(desktop));
</script>

{#snippet desktop()}
	{#snippet buttonForeground(view: Snippet)}
		<div class="button-foreground">
			{@render view()}
		</div>
	{/snippet}

	<div class="notification">
		<Button
			foreground={buttonForeground}
			onclick={async () => {
				$showNotifications = true;
			}}
		>
			<Icon icon="bell" />
		</Button>
	</div>
{/snippet}

<style lang="scss">
	div.notification {
		-webkit-app-region: no-drag;

		flex-direction: row;

		align-items: center;
	}

	div.button-foreground {
		padding: 8px;
	}
</style>
