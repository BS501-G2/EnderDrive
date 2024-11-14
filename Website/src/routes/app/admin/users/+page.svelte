<script lang="ts">
	import { useAdminContext } from '$lib/client/contexts/admin';
	import { onMount } from 'svelte';
	import Title from '../../title.svelte';

	const { pushSidePanel, pushTitle } = useAdminContext();

	onMount(() => pushTitle('Users'));
</script>

<div>aas</div>
