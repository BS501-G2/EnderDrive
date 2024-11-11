<script lang="ts">
	import { createAdminContext } from '$lib/client/contexts/admin';
	import type { Snippet } from 'svelte';

	const { children }: { children: Snippet } = $props();

    const {} = createAdminContext()
</script>

{@render children()}
