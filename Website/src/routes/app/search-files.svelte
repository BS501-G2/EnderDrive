<script
	lang="ts"
>
	import { goto } from '$app/navigation';
	import {
		useServerContext,
		type FileResource
	} from '$lib/client/client';
	import Button from '$lib/client/ui/button.svelte';
	import Icon from '$lib/client/ui/icon.svelte';
	import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte';
	import type { Snippet } from 'svelte';

	const {
		searchString,
		card,
		ondismiss
	}: {
		searchString: string;
		card: Snippet<
			[
				name: string,
				seeMore: () => void,
				snippet: Snippet
			]
		>;
		ondismiss: () => void;
	} =
		$props();

	const {
		me,
		getFiles
	} =
		useServerContext();
</script>

{@render card(
	'Files',
	() => {},
	main
)}

{#snippet main()}
	{#await (async () => {
		const files = getFiles(void 0, void 0, searchString, (await me()).id, void 0, 0, 10);

		return files;
	})()}
		<LoadingSpinner
			size="1em"
		/>
	{:then files}
		{#each files as file}
			{@render fileCard(
				file
			)}
		{/each}
	{/await}
{/snippet}

{#snippet fileCard(
	file: FileResource
)}
	{#snippet buttonForeground(
		view: Snippet
	)}
		<div
			class="button"
		>
			{@render view()}
		</div>
	{/snippet}

	<Button
		onclick={() => {
			goto(
				`/app/files?fileId=${file.id}`
			);
			ondismiss();
		}}
		foreground={buttonForeground}
	>
		<div
			class="icon"
		>
			<Icon
				icon="file"
				thickness="solid"
			/>
		</div>
		<div
			class="name"
		>
			{file.name}
		</div>
	</Button>
{/snippet}

<style
	lang="scss"
>
	@use '../../global.scss'
		as *;

	div.button {
		display: flex;
		text-align: left;
		flex-direction: row;
		align-items: center;

		flex-grow: 1;
		border: none;

		padding: 8px;
		gap: 8px;

		> div.name {
			flex-grow: 1;

			> p.name {
				font-size: 1.2em;
			}

			> p.username {
				font-size: 0.8em;
				font-weight: lighter;
			}
		}
	}
</style>
