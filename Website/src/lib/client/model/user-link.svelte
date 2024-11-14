<script lang="ts">
	import { onMount } from 'svelte';
	import { useServerContext, type UserResource } from '../client';
	import LoadingSpinner from '../ui/loading-spinner.svelte';

	const { userId }: { userId: string } = $props();
	const { getUser } = useServerContext();

	async function load(): Promise<UserResource | null> {
		const user = getUser(userId);

		return user;
	}

	let promise = $state(load());
</script>

{#await promise}
	<LoadingSpinner size="1rem" />
{:then user}
	{#if user}
		<a class="user" href="/app/profile?id={user.id}">@{user.username}</a>
	{:else}
		<p class="invalid">Invalid username</p>
	{/if}
{:catch error}
	<p class="invalid">{error.message}</p>
{/await}

<style lang="scss">
	a.user {
		color: inherit;
		text-decoration: none;
	}

	a.user:hover {
		text-decoration: underline;
	}

	a.user:visited {
		color: inherit;
	}

	p.invalid {
		color: red;
	}
</style>
