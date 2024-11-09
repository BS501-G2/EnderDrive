<script lang="ts">
	import { onMount, type Snippet } from 'svelte';
	import Overlay from '../overlay.svelte';
	import FileBrowserAction from './file-browser-action.svelte';
	import Separator from '$lib/client/ui/separator.svelte';
	import { useFileBrowserContext } from '$lib/client/contexts/file-browser';
	import { useAppContext } from '$lib/client/contexts/app';
	import type { IconOptions } from '$lib/client/ui/icon.svelte';
	import Button from '$lib/client/ui/button.svelte';
	import Icon from '$lib/client/ui/icon.svelte';
	import Input from '$lib/client/ui/input.svelte';
	import { useServerContext, type FileResource } from '$lib/client/client';

	const { file, ondismiss }: { file: FileResource; ondismiss: () => void } = $props();
	const { isMobile, isDesktop } = useAppContext();
	const { createFolder } = useServerContext();
	const { onFileId } = useFileBrowserContext();

	let tabs: { id: number; name: string; icon: IconOptions; snippet: Snippet }[] = $state([]);
	let tab: number = $state(0);

	function push(name: string, icon: IconOptions, snippet: Snippet) {
		const id = Math.random();
		tabs = [...tabs, { id, name, icon, snippet }];

		return () => {
			tabs = tabs.filter((a) => a.id !== id);
		};
	}

	onMount(() => push('New File', { icon: 'file' }, fileCreation));
	onMount(() => push('New Folder', { icon: 'folder' }, folderCreation));

	let newFolderName: string = $state('');
</script>

{#snippet action(
	name: string,
	onclick: (
		event: MouseEvent & { currentTarget: EventTarget & HTMLButtonElement }
	) => Promise<void>,
	isPrimary: boolean
)}
	{#snippet foreground(view: Snippet)}
		<div class="foreground" class:primary={isPrimary}>
			{@render view()}
		</div>
	{/snippet}

	<Button {onclick} {foreground}>
		<p>{name}</p>
	</Button>
{/snippet}

{#snippet fileCreation()}{/snippet}

{#snippet folderCreation()}
	<div class="form">
		<p>
			Create a new folder
			{#if newFolderName.trim()}
				named
			{/if}
			<b class="folder-name">{newFolderName}</b> inside
			<b class="folder-name">{file.name}</b>
		</p>
		<Input id="folder-name" type="text" name="Folder Name" bind:value={newFolderName} />
	</div>
	<div class="actions">
		{@render action(
			'Create',
			async (event: MouseEvent & { currentTarget: EventTarget & HTMLButtonElement }) => {
				const newFolder = await createFolder(file.id, newFolderName);
				ondismiss();

				onFileId?.(event, newFolder.id);
			},
			true
		)}
	</div>
{/snippet}

<Overlay {ondismiss}>
	{#snippet children(windowButtons: Snippet)}
		<div class="create">
			{#if $isDesktop}
				<div class="side">
					<h2>Create New</h2>

					{#each tabs as { id, name, icon }, index (id)}
						{#snippet desktopForeground(view: Snippet)}
							{#key tab === index}
								<div class="desktop-foreground" class:active={tab === index}>
									{@render view()}
								</div>
							{/key}
						{/snippet}

						<Button
							onclick={() => {
								tab = index;
							}}
							foreground={desktopForeground}
						>
							<Icon {...icon} thickness={tab === index ? 'solid' : 'regular'} />
							<p class="label">{name}</p>
						</Button>
					{/each}
				</div>

				<Separator vertical />
			{/if}

			<div class="main">
				{#if $isMobile}
					<div class="mobile-header">
						<div class="mobile-tabs">
							{#each tabs as { id, name, icon }, index (id)}
								<div class="mobile-tab">
									{#snippet tabForeground(view: Snippet)}
										<div class="foreground">
											{@render view()}
										</div>
									{/snippet}

									<Button
										foreground={tabForeground}
										onclick={() => {
											tab = index;
										}}
									>
										<Icon {...icon} />
										<p>{name}</p>
									</Button>

									{#if tab === index}
										<div class="indicator"></div>
									{/if}
								</div>
							{/each}
						</div>

						{@render windowButtons()}
					</div>
				{/if}

				{#if $isDesktop}
					{@render windowButtons()}
				{/if}

				<div class="content">
					{@render tabs[tab]?.snippet()}
				</div>
			</div>
		</div>
	{/snippet}
</Overlay>

<style lang="scss">
	@use '../../global.scss' as *;

	div.create {
		flex-direction: row;

		background-color: var(--color-9);
		color: var(--color-1);

		min-height: 50dvh;
		// box-shadow: 2px 2px 4px var(--color-10);

		> div.side {
			@include force-size(172px, &);

			padding: 8px;
			gap: 8px;

			> h2 {
				font-size: 1.2em;
				font-weight: bolder;
			}

			div.desktop-foreground {
				flex-grow: 1;
				flex-direction: row;
				align-items: center;

				line-height: 1em;

				padding: 8px;
				gap: 8px;

				p.label {
					flex-grow: 1;
				}
			}

			div.desktop-foreground.active {
				background-color: var(--color-1);
				color: var(--color-5);

				p.label {
					font-weight: bolder;
				}
			}
		}

		> div.main {
			flex-grow: 1;

			min-width: 50dvw;

			> div.mobile-header {
				flex-direction: row;

				> div.mobile-tabs {
					flex-direction: row;
					flex-grow: 1;
					flex-basis: 0;

					> div.mobile-tab {
						flex-grow: 1;

						div.foreground {
							flex-direction: row;
							align-items: center;

							line-height: 1em;
							gap: 8px;
							padding: 8px;
						}

						div.indicator {
							@include force-size(&, 1px);

							background-color: var(--color-1);
							align-self: stretch;
						}
					}
				}
			}

			> div.content {
				padding: 8px;
				gap: 8px;

				flex-grow: 1;

				> div.form {
					flex-grow: 1;
					gap: 8px;
				}

				> div.actions {
					flex-direction: row;
					justify-content: flex-end;
				}
			}
		}
	}

	b.folder-name {
		font-weight: bolder;
	}

	div.foreground {
		padding: 8px;
	}

	div.foreground.primary {
		background-color: var(--color-1);
		color: var(--color-5);
	}
</style>
