<script lang="ts" module>
	export type FrameCallback<T> = (previousTime: number | null, time: number, value?: T) => T;
</script>

<script lang="ts" generics="T extends any">
	import { onMount, type Snippet } from 'svelte';

	const { children, callback }: { children?: Snippet; callback: FrameCallback<T> } = $props();

	onMount(() => {
		let previousTime: number | null = null;
		let value: T | null = null;
		let animationFrameRequest: number | null = null;

		const run = () => {
			const currentTime = Date.now();

			value = callback(previousTime, currentTime, value ?? undefined);
			previousTime = currentTime;

			animationFrameRequest = requestAnimationFrame(run);
		};

		run();

		return () => {
			if (animationFrameRequest != null) {
				cancelAnimationFrame(animationFrameRequest);
			}
		};
	});
</script>

{#if children != null}
	{@render children()}
{/if}
