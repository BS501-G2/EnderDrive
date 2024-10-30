<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import Icon, { type IconProps } from '$lib/ui/icon.svelte';
</script>

{#snippet navigationEntry(name: string, icon: IconProps, path: string)}
	<button
		class="navigation-entry"
		class:selected={$page.url.pathname === path}
		on:click={() => goto(path)}
	>
		<!-- <i class="fa-solid {icon}"></i> -->
		<Icon {...icon} size="1.2em" />

		<p>{name}</p>
	</button>
{/snippet}

<div class="navigation-list">
	{@render navigationEntry(
		'Manage Users',
		{ icon: 'user', thickness: 'solid' },
		'/app/admin/users'
	)}
	{@render navigationEntry(
		'View File Access Logs',
		{ icon: 'file-lines', thickness: 'solid' },
		'/app/admin/logs'
	)}
	{@render navigationEntry(
		'Bulletin',
		{ icon: 'newspaper', thickness: 'solid' },
		'/app/admin/news'
	)}
</div>

<style lang="scss">
	div.navigation-list {
		display: flex;
		flex-direction: column;

		gap: 8px;
	}

	button.navigation-entry {
		display: flex;
		flex-direction: row;
		border: none;

		background-color: unset;
		color: inherit;

		padding: 8px;
		gap: 8px;
		border-radius: 8px;

		> i {
			font-size: 1.2em;
		}

		> p {
			flex-grow: 1;

			text-align: start;
		}
	}

	button.navigation-entry:hover {
		cursor: pointer;
	}

	button.navigation-entry.selected {
		background-color: var(--primaryContainer);
		color: var(--onPrimaryContainer);
	}
</style>
