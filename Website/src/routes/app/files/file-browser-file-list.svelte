<script lang="ts">
  import { useFileBrowserContext, type CurrentFile } from '$lib/client/contexts/file-browser'
  import { createFileBrowserListContext } from '$lib/client/contexts/file-browser-list'
  import { onMount } from 'svelte'
  import FileBrowserFileListEntry from './file-browser-file-list-entry.svelte'
  import FileBrowserAction from './file-browser-action.svelte'
  import { useServerContext } from '$lib/client/client'
  import FileBrowserRefresh from './file-browser-refresh.svelte'
  import Title from '../title.svelte'
  import { useAppContext } from '$lib/client/contexts/app'
  import FileBrowserActions from './file-browser-actions.svelte'

  const {
    current
  }: {
    current: CurrentFile & {
      type: 'folder' | 'shared' | 'starred' | 'trash'
    }
  } = $props()

  const { setFileListContext, refresh, selectMode } = useFileBrowserContext()
  const { getFileMime, setFileStar, getFileStar } = useServerContext()
  const { context, selectedFileIds } = createFileBrowserListContext()
  const { isMobile } = useAppContext()

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
      {#each current.files as file}
        {#if file.type === 'folder'}
          <FileBrowserFileListEntry {file} />
        {:else if selectMode}
          {#if selectMode.allowedFileMimeTypes.length !== 0}
            {#await getFileMime(file.file.id) then mime}
              {#if selectMode.allowedFileMimeTypes.some( (mimeType) => (mimeType instanceof RegExp ? mimeType.test(mime) : mimeType === mime) )}
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
    }
  }
</style>
