<script lang="ts">
	import Overlay from '$lib/widgets/overlay.svelte';
	import { useTutorial } from './tutorial-host.svelte';

	const {
		id,
		title,
		message,
		hotspot
	}: {
		id: string;
		title: string;
		message: string;
		hotspot: HTMLElement[]
	} = $props();

	const tutorial = useTutorial(id);
</script>

<Overlay dim onDismiss={$tutorial.dismiss}>
	<p>a</p>
</Overlay>
