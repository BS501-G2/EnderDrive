<script lang="ts">
	import type { Snippet } from 'svelte';
	import App from '../ui/app.svelte';

	const { children }: { children: Snippet } = $props();
</script>

<App>
	{@render children()}
</App>
