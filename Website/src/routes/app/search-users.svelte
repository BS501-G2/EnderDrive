<script lang="ts">
	import { useClientContext } from '$lib/client/client';
	import { type UserResource } from '$lib/client/client-server-side-request';
	import Button from '$lib/client/ui/button.svelte';
	import Icon from '$lib/client/ui/icon.svelte';
	import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte';
	import type { Snippet } from 'svelte';

	const {
		functions: { getUsers }
	} = useClientContext();

	const {
		searchString,
		card
	}: {
		searchString: string;
		card: Snippet<[name: string, seeMore: () => void, snippet: Snippet]>;
	} = $props();
</script>

{@render card('Users', () => {}, main)}

{#snippet user(user: UserResource)}
	{#snippet buttonForeground(view: Snippet)}
		<div class="button">
			{@render view()}
		</div>
	{/snippet}

	<Button onclick={() => {}} foreground={buttonForeground}>
		<div class="icon">
			<Icon icon="user" thickness="solid" />
		</div>
		<div class="name">
			<p class="name">{user.firstName} {user.middleName} {user.lastName}</p>
			<p class="username">@{user.username}</p>
		</div>
	</Button>
{/snippet}

{#snippet main()}
	{#await getUsers(searchString)}
		<LoadingSpinner size="1em" />
	{:then users}
		{#each users as entry}
			{@render user(entry)}
		{/each}
		{#each users as entry}
			{@render user(entry)}
		{/each}
		{#each users as entry}
			{@render user(entry)}
		{/each}
		{#each users as entry}
			{@render user(entry)}
		{/each}
		{#each users as entry}
			{@render user(entry)}
		{/each}
		{#each users as entry}
			{@render user(entry)}
		{/each}
		{#each users as entry}
			{@render user(entry)}
		{/each}
		{#each users as entry}
			{@render user(entry)}
		{/each}
		{#each users as entry}
			{@render user(entry)}
		{/each}
		{#each users as entry}
			{@render user(entry)}
		{/each}
		{#each users as entry}
			{@render user(entry)}
		{/each}
		{#each users as entry}
			{@render user(entry)}
		{/each}
	{/await}
{/snippet}

<style lang="scss">
	@use '../../global.scss' as *;

	div.button {
		display: flex;
		text-align: left;
		flex-direction: row;
		align-items: center;

		border: none;

		padding: 8px;
		gap: 8px;

		> div.name {
			flex-grow: 1;

			> p.name {
				font-size: 1.2em;
			}

			> p.username {
				font-size: 0.8em;
				font-weight: lighter;
			}
		}
	}
</style>
