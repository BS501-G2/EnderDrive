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

	const { ondismiss }: { ondismiss: () => void } = $props();
	const { isMobile, isDesktop } = useAppContext();

	let tabs: { id: number; name: string; icon: IconOptions; snippet: Snippet }[] = $state([]);
	let tab: number = $state(0);

	function push(name: string, icon: IconOptions, snippet: Snippet) {
		const id = Math.random();
		tabs = [...tabs, { id, name, icon, snippet }];

		return () => {
			tabs = tabs.filter((a) => a.id !== id);
		};
	}

	onMount(() => push('New File', { icon: 'file' }, createFolder));
	onMount(() => push('New Folder', { icon: 'folder' }, createFile));
</script>

{#snippet createFile()}{/snippet}

{#snippet createFolder()}{/snippet}

<Overlay {ondismiss}>
	{#snippet children(windowButtons: Snippet)}
		<div class="create">
			{#if $isDesktop}
				<div class="side">
					<h2>Create New</h2>

					{#each tabs as { id, name, icon }, index (id)}

						
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

										{#if tab === index}
											<div class="indicator"></div>
										{/if}
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
								</div>
							{/each}
						</div>

						{@render windowButtons()}
					</div>
				{/if}

				{#if $isDesktop}
					{@render windowButtons()}
				{/if}
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
		box-shadow: 2px 2px 4px var(--color-10);

		> div.side {
			@include force-size(172px, &);

			padding: 8px;

			> h2 {
				font-size: 1.2em;
				font-weight: bolder;
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

					> div.mobile-tab {
						flex-grow: 1;

						div.foreground {
							flex-direction: row;
							flex-grow: 1;
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
		}
	}
</style>
