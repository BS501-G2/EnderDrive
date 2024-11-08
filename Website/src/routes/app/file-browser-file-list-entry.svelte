<script lang="ts">
	import { FileType, useServerContext } from '$lib/client/client';
	import type { FileEntry } from '$lib/client/contexts/file-browser';
	import { useFileBrowserListContext } from '$lib/client/contexts/file-browser-list';
	import Button from '$lib/client/ui/button.svelte';
	import Icon from '$lib/client/ui/icon.svelte';
	import { onMount, type Snippet } from 'svelte';
	import { writable, type Writable } from 'svelte/store';

	const { file }: { file: FileEntry } = $props();
	const { pushFile } = useFileBrowserListContext();
	const { getFileContents, getFileSnapshots } = useServerContext();

	let buttonElement: HTMLElement = $state(null as never);
	let hover: boolean = $state(false);
	const selected: Writable<boolean> = writable(false);

	onMount(() => pushFile(file, buttonElement, selected));
</script>

<div
	bind:this={buttonElement}
	class="file"
	onmouseenter={() => {
		hover = true;
	}}
	onmouseleave={() => {
		hover = false;
	}}
	role="figure"
>
	<div class="check">
		{#if hover || $selected}
			<button
				class="check"
				onclick={() => {
					$selected = !$selected;
				}}
			>
				{#if $selected}
					<Icon icon="circle-check" size="18px" />
				{:else}
					<Icon icon="circle" size="18px" />
				{/if}
			</button>
		{/if}
	</div>
	<div class="preview">
		{#if file.file.type === FileType.Folder}
			<Icon icon="folder" size="32px" />
		{:else if file.file.type === FileType.File}
			<Icon icon="file" size="32px" />
		{/if}
	</div>

	<div class="name">
		<a href="/app/files?fileId={file.file.id}">
			{file.file.name}
		</a>

		{#if hover}
			<div class="actions">
				{#snippet buttonForeground(view: Snippet)}
					<div class="foreground">
						{@render view()}
					</div>
				{/snippet}

				<Button onclick={() => {}} foreground={buttonForeground}>
					<Icon icon="folder" size="1em" />
				</Button>
			</div>
		{/if}
	</div>

	<div class="modified">
		{#if file.file.type}
			{#await getFileContents(file.file.id) then fileContents}
				{JSON.stringify(fileContents)}
			{/await}
		{/if}
	</div>

	<div class="date"></div>
</div>

<style lang="scss">
	@use '../../global.scss' as *;

	div.file {
		color: var(--color-1);

		flex-direction: row;
		align-items: center;

		padding: 8px;
		gap: 8px;

		> div.check {
			@include force-size(32px, 32px);

			align-items: center;
			justify-content: center;

			> button {
				border: none;
				text-decoration: none;

				cursor: pointer;
			}
		}

		> div.name {
			flex-grow: 1;
			min-width: 512px;

			flex-direction: row;
			align-items: center;

			> a {
				text-decoration: none;
				color: inherit;

				flex-grow: 1;
			}

			> div.actions {
				div.foreground {
					padding: 8px;
				}
			}
		}
	}

	div.file:hover {
		> div.name {
			> a {
				text-decoration: underline;
			}
		}

		background-color: var(--color-5);
	}
</style>
