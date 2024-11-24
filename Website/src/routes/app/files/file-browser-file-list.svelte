<script lang="ts">
  import {
    useFileBrowserContext,
    type CurrentFile,
    type FileEntry
  } from '$lib/client/contexts/file-browser'
  import { createFileBrowserListContext } from '$lib/client/contexts/file-browser-list'
  import { onMount } from 'svelte'
  import FileBrowserFileListEntry from './file-browser-file-list-entry.svelte'
  import FileBrowserAction from './file-browser-action.svelte'
  import { useServerContext } from '$lib/client/client'
  import FileBrowserRefresh from './file-browser-refresh.svelte'
  import Title from '../title.svelte'
  import { useAppContext } from '$lib/client/contexts/app'
  import FileBrowserActions from './file-browser-actions.svelte'
  import type { Writable } from 'svelte/store'
  import Icon from '$lib/client/ui/icon.svelte'

  const {
    displayedFiles,
    current
  }: {
    displayedFiles: FileEntry[]
    current: CurrentFile & {
      type: 'folder' | 'shared' | 'starred' | 'trash' | 'loading'
    }
  } = $props()

  const { setFileListContext, refresh, selectMode } = useFileBrowserContext()
  const { getFileMime, setFileStar, getFileStar } = useServerContext()
  const { context, selectedFileIds } = createFileBrowserListContext()
  const { isMobile, isDesktop } = useAppContext()

  onMount(() => setFileListContext(context))
</script>

<FileBrowserRefresh />

{#if $selectedFileIds.length > 0}
  <Title title="{$selectedFileIds.length} Selected" />
{/if}

<FileBrowserActions {current} {selectedFileIds} />

<div class="container">
  <div class="list-header"></div>

  <div class="list-container">
    <div class="header"></div>
    <div class="list" class:mobile={$isMobile}>
      {#snippet list(files: FileEntry[])}
        {#if $isDesktop}
          <FileBrowserFileListEntry head />
        {/if}
          {#each files as file}
            {#if file.type === 'folder'}
              <FileBrowserFileListEntry {file} />
            {:else if selectMode}
              {#if selectMode.allowedFileMimeTypes.length !== 0}
                {#await (async () => {
                  const mime = await getFileMime(file.file.id)

                  return { mime, filter: await selectMode.filter(file) }
                })() then { mime, filter }}
                
                  {#if filter && selectMode.allowedFileMimeTypes.some( (mimeType) => (mimeType instanceof RegExp ? mimeType.test(mime) : mimeType === mime) )}
                    <FileBrowserFileListEntry {file} />
                  {/if}
                {/await}
              {:else}
                <FileBrowserFileListEntry {file} />
              {/if}
            {:else}
              <FileBrowserFileListEntry {file} />
            {/if}
          {/each}

        {#if !files.length}
          <div class="empty">
            <Icon icon="folder-open" thickness="solid" size="4rem" />
            <p>This list is empty.</p>
          </div>
        {/if}
      {/snippet}

      {#if current.type !== 'loading'}
        {@render list(current.files)}
      {:else}
        {@render list(displayedFiles)}
      {/if}
    </div>
  </div>
</div>

<style lang="scss">
  div.container {
    flex-grow: 1;

    min-height: 0;
    min-width: 0;

    div.list-container {
      flex-grow: 1;

      overflow: auto;
      min-height: 0;
      min-width: 0;

      > div.list.mobile {
      }

      > div.list {
        flex-grow: 1;
      }

      div.empty {
        gap: 32px;
        flex-grow: 1;

        align-items: center;
        justify-content: center;

        color: gray;
      }
    }
  }
</style>
