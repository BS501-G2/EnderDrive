<script lang="ts">
	import { FileType, useServerContext } from '$lib/client/client';
	import type { FileEntry } from '$lib/client/contexts/file-browser';
	import { useFileBrowserListContext } from '$lib/client/contexts/file-browser-list';
	import Button from '$lib/client/ui/button.svelte';
	import Icon from '$lib/client/ui/icon.svelte';
	import { onMount, type Snippet } from 'svelte';

	const { file }: { file: FileEntry } = $props();
	const { pushFile, selectedFileIds } = useFileBrowserListContext();
	const { getFileContents, getFileSnapshots } = useServerContext();

	let buttonElement: HTMLElement = $state(null as never);
	let hover: boolean = $state(false);

	onMount(() => pushFile(file, buttonElement));
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
		{#if hover || $selectedFileIds.includes(file.file.id)}
			<button
				class="check"
				onclick={() => {
					$selectedFileIds = $selectedFileIds.includes(file.file.id)
						? $selectedFileIds.filter((id) => id !== file.file.id)
						: [...$selectedFileIds, file.file.id];
				}}
			>
				{#if $selectedFileIds.includes(file.file.id)}
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
			<p>
				{file.file.name}
			</p>
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

		overflow: hidden;

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
			min-width: 172px;

			flex-direction: row;
			align-items: center;

			> a {
				text-decoration: none;

				color: inherit;

				min-width: 0;

				flex-grow: 1;

				> p {
					text-overflow: ellipsis;
					text-wrap: nowrap;

					overflow: hidden;
				}
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
