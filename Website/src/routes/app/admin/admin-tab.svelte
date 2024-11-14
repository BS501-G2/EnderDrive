<script lang="ts">
	import { useAdminContext, type Tab } from '$lib/client/contexts/admin';
	import { onMount } from 'svelte';

	const { name, callback }: { name: string; callback: Tab['callback'] } = $props();

	const { pushTab } = useAdminContext();

	onMount(() => pushTab(name, callback));
</script>
