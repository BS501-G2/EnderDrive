<script lang="ts">
  import { useClientContext } from '$lib/client/client'
  import type { FileResource } from '$lib/client/resource'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import Overlay from '../../overlay.svelte'
  import FileBrowserPathMenuEntry from './file-browser-path-menu-entry.svelte'

  const {
    button,
    file,
    ondismiss,
    cascade = false
  }: {
    button: HTMLButtonElement
    file: FileResource
    ondismiss: () => void
    cascade?: boolean
  } = $props()
  const { server } = useClientContext()

  const entryBounds = $derived(button.getBoundingClientRect())

  const x = $derived(cascade ? entryBounds.x + entryBounds.width : entryBounds.x)
  const y = $derived(cascade ? entryBounds.y : entryBounds.y + entryBounds.height)
</script>

<Overlay {x} {y} {ondismiss} nodim>
  <div class="menu">
    {#await server.GetFiles({ ParentFolderId: cascade ? file.Id : file.ParentId })}
      <div class="loading">
        <LoadingSpinner size="3em" />
      </div>
    {:then files}
      {#if files.length > 0}
        <div class="list">
          {#each files as file}
            <FileBrowserPathMenuEntry {file} />
          {/each}
        </div>
      {:else}
        <div class="empty">(empty)</div>
      {/if}
    {/await}
  </div>
</Overlay>

<style lang="scss">
  @use '../../../global.scss' as *;

  div.menu {
    background-color: var(--color-9);
    color: var(--color-1);
    // box-shadow: 2px 2px 4px var(--color-10);

    overflow: hidden auto;

    > div.loading {
      align-items: center;
      justify-content: center;

      padding: 16px;
    }

    > div.empty {
      align-items: center;
      justify-content: center;

      line-height: 1em;
      padding: 8px;
    }
  }
</style>
