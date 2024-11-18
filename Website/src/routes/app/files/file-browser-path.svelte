<script lang="ts">
  import { useServerContext } from '$lib/client/client'
  import {
    useFileBrowserContext,
    type CurrentFile
  } from '$lib/client/contexts/file-browser'
  import Button from '$lib/client/ui/button.svelte'
  import Icon from '$lib/client/ui/icon.svelte'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import { onMount, type Snippet } from 'svelte'
  import FileBrowserPathEntry from './file-browser-path-entry.svelte'

  const { pushTop, onFileId } = useFileBrowserContext()

  const {
    current
  }: {
    current: CurrentFile & {
      type: 'file' | 'folder' | 'loading'
    }
  } = $props()

  onMount(() => pushTop(content))
</script>

{#snippet content()}
  {#snippet loading()}
    <div class="loading">
      <LoadingSpinner size="1em" />
      <p>Loading</p>
    </div>
  {/snippet}

  <div class="path-container">
    {#if current.type === 'loading'}
      {@render loading()}
    {:else if current.path != null}
      {@const root = current.path[0]}

      {#snippet rootForeground(view: Snippet)}
        <div class="root">
          {@render view()}
        </div>
      {/snippet}

      {#snippet rootBackground(view: Snippet)}
        <div class="root-background">
          {@render view()}
        </div>
      {/snippet}

      <Button
        background={rootBackground}
        foreground={rootForeground}
        onclick={(event) => {
          onFileId?.(event, root.id)
        }}
      >
        {#if root.ownerUserId}
          <Icon icon="folder" />
          <p>My Files</p>
        {:else}
          <Icon icon="user" />
          <p>Root</p>
        {/if}
      </Button>

      <div class="path">
        {#each current.path.slice(1) as file (file.id)}
          <FileBrowserPathEntry {file} />
        {/each}
      </div>
    {/if}
  </div>
{/snippet}

<style lang="scss">
  @use '../../../global.scss' as *;

  div.path-container {
    min-height: 32px;

    flex-direction: row;

    min-width: 0;

    > div.loading {
      padding: 0 8px;
      gap: 8px;

      flex-direction: row;
      align-items: center;
    }

    div.root-background {
      flex-direction: row;
      align-items: center;

      flex-shrink: 0;
      flex-grow: 1;

      background-color: var(--color-5);
      color: var(--color-1);
    }

    div.root {
      flex-grow: 1;
      flex-shrink: 0;

      align-items: center;
      flex-direction: row;

      padding: 0 8px;
      gap: 8px;
    }

    > div.path {
      flex-direction: row;

      overflow: auto hidden;

      padding: 0 8px;

      min-width: 0;

      ::-webkit-scrollbar {
        scrollbar-width: 0;
      }
    }
  }
</style>
