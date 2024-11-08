<script lang="ts">
	import { useDashboardContext } from '$lib/client/contexts/dashboard';
	import { derived, type Readable } from 'svelte/store';
	import AppButton from './app-button.svelte';
	import { useFileBrowserContext, type FileBrowserAction } from '$lib/client/contexts/file-browser';
	import { onMount } from 'svelte';
	import Separator from '$lib/client/ui/separator.svelte';

	const { actions }: { actions: Readable<FileBrowserAction[]> } = $props();
	const { pushBottom } = useFileBrowserContext();

	const appButtons = derived(actions, (actions) =>
		actions.filter((action) => action.type === 'left-main' || action.type === 'right-main')
	);

	const buttons = derived(actions, (actions) => {
		const left = actions.filter((action) => action.type === 'left');
		const right = actions.filter((action) => action.type === 'right').slice(0);

		while (left.length > 3) {
			right.unshift(left.pop()!);
		}

		if (right.length === 1) {
			left.push(right.shift()!);
		}

		return [left, right];
	});

	onMount(() => pushBottom(button));
</script>

{#each $appButtons as { id, icon, label, onclick } (id)}
	<AppButton {icon} {label} {onclick} />
{/each}

{#snippet button()}
	<div class="actions">
		{#each $buttons[0] as { id, snippet }, index (id)}
			{#if index != 0}
				<Separator vertical />
			{/if}

			<div class="button">
				{@render snippet()}
			</div>
		{/each}
	</div>
{/snippet}

<style lang="scss">
	div.actions {
		flex-direction: row;
		flex-grow: 1;

		> div.button {
			flex-grow: 1;
		}
	}
</style>
