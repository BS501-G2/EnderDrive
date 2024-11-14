<script lang="ts">
	import { useAdminContext } from '$lib/client/contexts/admin';
	import type { IconOptions } from '$lib/client/ui/icon.svelte';
	import { onMount, type Snippet } from 'svelte';

	const { name, icon, children }: { name: string; icon: IconOptions; children: Snippet } = $props();
	const { pushSidePanel } = useAdminContext();

	onMount(() => pushSidePanel(name, icon, children));
</script>
