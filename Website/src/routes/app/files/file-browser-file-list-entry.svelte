<script lang="ts">
	import { FileType, useServerContext } from '$lib/client/client';
	import { useAppContext } from '$lib/client/contexts/app';
	import type { FileEntry } from '$lib/client/contexts/file-browser';
	import { useFileBrowserListContext } from '$lib/client/contexts/file-browser-list';
	import Button from '$lib/client/ui/button.svelte';
	import Icon from '$lib/client/ui/icon.svelte';
	import { onMount, type Snippet } from 'svelte';
	import { get } from 'svelte/store';

	const { file }: { file: FileEntry } = $props();
	const { pushFile, selectedFileIds, selectFile, deselectFile } = useFileBrowserListContext();
	const { getFileContents, getFileSnapshots } = useServerContext();
	const { isMobile, isDesktop } = useAppContext();

	let fileElement: HTMLElement = $state(null as never);
	let hover: boolean = $state(false);

	onMount(() => pushFile(file, fileElement));
</script>

<!-- svelte-ignore a11y_interactive_supports_focus -->
<!-- svelte-ignore a11y_no_static_element_interactions -->
<div
	bind:this={fileElement}
	oncontextmenu={(event) => {
		if (get(isMobile)) {
			event.preventDefault();

			$selectedFileIds.includes(file.file.id)
				? deselectFile(file.file.id)
				: selectFile(file.file.id);
		}
	}}
	onclick={(event) => {
		if (get(isMobile) && $selectedFileIds.length) {
			event.preventDefault();

			$selectedFileIds.includes(file.file.id)
				? deselectFile(file.file.id)
				: selectFile(file.file.id);
		}
	}}
	class="file"
	class:mobile={$isMobile}
	onmouseenter={() => {
		hover = true;
	}}
	onmouseleave={() => {
		hover = false;
	}}
	onkeypress={() => {}}
>
	{#if $isDesktop || ($isMobile && $selectedFileIds.length)}
		<div class="check">
			{#if hover || $selectedFileIds.includes(file.file.id)}
				<button
					class="check"
					onclick={() => {
						$selectedFileIds.includes(file.file.id)
							? deselectFile(file.file.id)
							: selectFile(file.file.id);
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
	{/if}

	<div class="preview">
		{#if file.file.type === FileType.Folder}
			<Icon icon="folder" size="32px" />
		{:else if file.file.type === FileType.File}
			<Icon icon="file" size="32px" />
		{/if}
	</div>

	<div class="name">
		<div class="file-name">
			<a href="/app/files?fileId={file.file.id}" class:mobile={$isMobile}>
				<p class:mobile={$isMobile}>
					{file.file.name}
				</p>
			</a>
		</div>

		{#if (hover || $isMobile) && ($isDesktop || $selectedFileIds.length === 0)}
			<div class="actions">
				{#snippet buttonForeground(view: Snippet)}
					<div class="foreground">
						{@render view()}
					</div>
				{/snippet}

				{#if $isDesktop}
					<Button onclick={() => {}} foreground={buttonForeground}>
						<Icon icon="folder" size="1em" />
					</Button>
				{:else if $isMobile}
					<Button onclick={() => {}} foreground={buttonForeground}>
						<Icon icon="ellipsis-vertical" thickness="solid" size="1em" />
					</Button>
				{/if}
			</div>
		{/if}
	</div>

	{#if $isDesktop}
		<div class="modified">
			{#await getFileContents(file.file.id) then fileContents}
				{JSON.stringify(fileContents)}
			{/await}
		</div>

		<div class="date"></div>
	{/if}
</div>

<style lang="scss">
	@use '../../../global.scss' as *;

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

			> div.file-name {
				flex-direction: row;
				flex-grow: 1;

				min-width: 0;

				> a {
					text-decoration: none;

					color: inherit;

					min-width: 0;

					> p {
						text-overflow: ellipsis;
						text-wrap: nowrap;

						overflow: hidden;
					}

					> p.mobile {
						padding: 16px 0;
					}
				}

				> a.mobile {
					flex-grow: 1;
				}
			}

			> div.actions {
				div.foreground {
					padding: 8px;
				}
			}
		}
	}

	div.file.mobile {
		padding: 0 8px;
	}

	div.file:hover {
		background-color: var(--color-5);

		> div.name {
			> div.file-name {
				> a:not(.mobile) {
					text-decoration: underline;
				}
			}
		}

		> div.modified {
			flex-shrink: 0;
		}
	}
</style>
