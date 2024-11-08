<script lang="ts">
	import { onMount } from 'svelte';
	import {  useServerContext, type UserResource } from '../client';
	import LoadingSpinner from '../ui/loading-spinner.svelte';

	const { userId }: { userId: string } = $props();
	const { getUser } = useServerContext();

	let state:
		| { state: 'exists'; user: UserResource }
		| { state: 'error'; error: Error }
		| { state: 'loading' }
		| null = $state(null);

	onMount(async () => {
		state = { state: 'loading' };
	});
</script>

{#await getUser(userId)}
	<LoadingSpinner size="1rem" />
{/await}
<div class="user"></div>

<style lang="scss">
	div.user {
		background-color: var(--color-1);
	}
</style>
