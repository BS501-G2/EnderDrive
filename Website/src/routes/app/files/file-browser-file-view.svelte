<script lang="ts">
  import { onMount, type Snippet } from 'svelte'
  import Overlay from '../../overlay.svelte'
  import FileBrowserRefresh from './file-browser-refresh.svelte'
  import { useAppContext } from '$lib/client/contexts/app'
  import FileBrowserFileContent from './file-browser-file-content.svelte'
  import { useFileBrowserContext } from '$lib/client/contexts/file-browser'
  import { createFileBrowserListContext } from '$lib/client/contexts/file-browser-list'
  import type { FileResource } from '$lib/client/resource'

  const {
    file
  }: {
    file: FileResource
  } = $props()
  const { setFileListContext } = useFileBrowserContext()

  const { isMobile, isDesktop } = useAppContext()
  const { selectedFileIds, context } = createFileBrowserListContext()

  onMount(() => {
    const ondestroy = setFileListContext(context)
    selectedFileIds.set([file.Id])

    return ondestroy
  })
</script>

<FileBrowserRefresh />

{#if $isMobile}
  <Overlay ondismiss={() => window.history.back()} x={0} y={0}>
    {#snippet children(windowButtons: Snippet)}
      <div class="overlay">
        <div class="header">
          <div class="title">
            <p>
              {file?.Name}
            </p>
          </div>

          {@render windowButtons()}
        </div>

        <div class="main">
          <FileBrowserFileContent fileId={file.Id} {selectedFileIds} />
        </div>
      </div>
    {/snippet}
  </Overlay>
{/if}

{#if $isDesktop}
  <div class="content">
    <FileBrowserFileContent fileId={file.Id} {selectedFileIds} />
  </div>
{/if}

<style lang="scss">
  @use '../../../global.scss' as *;

  div.content {
    // @include force-size(100dvw, 100dvh);
    flex-grow: 1;
  }

  div.overlay {
    @include force-size(100dvw, 100dvh);

    background-color: var(--color-10);
    color: var(--color-5);

    > div.header {
      flex-direction: row;
      align-items: center;

      min-width: 0;

      > div.title {
        flex-direction: row;

        margin: 8px;
        font-weight: bolder;
        flex-grow: 1;
        min-width: 0;

        > p {
          overflow: hidden;

          text-wrap: nowrap;
          text-overflow: ellipsis;
          font-size: 1.2em;
          min-width: 0;
        }
      }
    }

    > div.main {
      flex-grow: 1;
    }
  }
</style>
