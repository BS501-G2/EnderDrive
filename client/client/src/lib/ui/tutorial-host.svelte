<script module lang="ts">
	const tutorial: Writable<Record<string, boolean>> = persisted('tutorials', {});
	export interface Tutorial {
		name: string;
		content: Snippet;
		points: HTMLElement[];
	}

	const tutorials: Writable<Tutorial[]> = writable([]);

	export function useTutorial(name: string): Readable<{ show: boolean; dismiss: () => void }> {
		return derived(tutorial, (tutorial) => {
			const { [name]: show = false } = tutorial;

			return {
				show,
				dismiss: () => {
					tutorial[name] = true;
				}
			};
		});
	}

	export function pushTutorial(name: string, content: Snippet, points: HTMLElement[]): () => void {
		const tutorial: Tutorial = {
			name,
			content,
			points
		};

		tutorials.update((value) => {
			value.push(tutorial);

			return value;
		});

		return () =>
			tutorials.update((value) => {
				const index = value.indexOf(tutorial);
				if (index !== -1) {
					value.splice(index, 1);
				}

				return value;
			});
	}
</script>

<script lang="ts">
	import type { Snippet } from 'svelte';
	import type { Readable } from 'svelte/motion';
	import { derived, writable, type Writable } from 'svelte/store';
	import { persisted } from 'svelte-persisted-store';

	const { id, title, message }: { id: string; title: string; message: string } = $props();
	const tutorial = useTutorial(id);
</script>

{#each $tutorials as tutorial}

{/each}

<style lang="scss"></style>
