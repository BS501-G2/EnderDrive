<script lang="ts">
	import { onMount, type Snippet } from 'svelte';
	import { ClientStateType, useClientContext } from '../client';
	import Button from './button.svelte';
	import LoadingSpinner from './loading-spinner.svelte';
	import Banner from './banner.svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';

	const {
		client,
		functions: { getSetupRequirements }
	} = useClientContext();

	const { children }: { children: Snippet } = $props();

	onMount(async () => {
		const response = await getSetupRequirements({});

		if (response.AdminSetupRequired) {
			await goto(`/setup?return=${encodeURIComponent(`${$page.url.pathname}${$page.url.search}`)}`);
		}
	});
</script>

{#if $client.state === ClientStateType.Connected}
	{@render children()}
{:else if $client.state === ClientStateType.Connecting}
	<div class="splash-container">
		<div class="splash">
			<LoadingSpinner size="3rem" />
		</div>
	</div>
{:else if $client.state === ClientStateType.Failed}
	<div class="splash-container error">
		<div class="splash">
			<Banner type="error" icon={{ icon: 'xmark', thickness: 'solid', size: '1.5em' }}>
				{#snippet content()}
					<p>{$client.error ?? 'Unknown error'}</p>
				{/snippet}

				{#snippet bottom()}
					{#snippet container(view: Snippet)}
						<div class="retry-button">
							{@render view()}
						</div>
					{/snippet}
					<Button background={container} onclick={() => $client.retry()}>Retry</Button>
				{/snippet}
			</Banner>
		</div>
	</div>
{/if}

<style lang="scss">
	div.splash-container {
		flex-grow: 1;

		display: flex;

		align-items: center;
		justify-content: center;
	}

	div.error-message {
		color: var(--color-6);
	}

	div.splash {
		flex-direction: column;
		align-items: center;
		justify-content: center;

		gap: 8px;
	}

	div.retry-button {
		padding: 8px 16px;

		border: solid 1px var(--color-1);
		border-radius: 8px;
	}
</style>
