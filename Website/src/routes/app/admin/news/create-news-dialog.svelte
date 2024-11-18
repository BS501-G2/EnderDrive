<script lang="ts">
  import { type Snippet } from 'svelte'
  import Overlay from '../../../overlay.svelte'
  import Button from '$lib/client/ui/button.svelte'
  import Icon from '$lib/client/ui/icon.svelte'
  import FileSelector from '../../files/file-selector.svelte'
  import { useServerContext, type FileResource } from '$lib/client/client'

  const {
    newsId,
    ondismiss
  }: {
    newsId?: number
    ondismiss: () => void
  } = $props()

  const { getFileMime } = useServerContext()
  let browse: [fileId: string | null] | null = $state(null)
</script>

{#if browse != null}
  <Overlay
    ondismiss={() => {
      browse = null
    }}
  >
    {#snippet children(windowButtons: Snippet)}
      <div class="file-browser">
        <div class="header">
          <h2>Upload Image</h2>

          {@render windowButtons()}
        </div>

        <div class="main">
          <p>Please select an image to be used as the news cover.</p>

          <FileSelector
            maxFileCount={1}
            onresult={async (files: FileResource[]) => {
              const fileMime = await getFileMime(files[0].id)

              console.log(fileMime)
              browse = null
            }}
            oncancel={() => {
              browse = null
            }}
            mimeTypes={[new RegExp('^image/.*$')]}
          />
        </div>
      </div>
    {/snippet}
  </Overlay>
{:else}
  <Overlay {ondismiss}>
    {#snippet children(windowButtons: Snippet)}
      <div class="create-dialog">
        <div class="header">
          <h2>Create News</h2>

          {@render windowButtons()}
        </div>

        <div class="body">
          <p>The website allows you to create a news by uploading images.</p>

          {#snippet foreground(view: Snippet)}
            <div class="foreground">
              {@render view()}
            </div>
          {/snippet}

          <Button {foreground} onclick={() => {}}>
            <Icon icon="upload" thickness="solid" />
            <p>Upload Image</p>
          </Button>

          <Button
            {foreground}
            onclick={() => {
              browse = [null]
            }}
          >
            <Icon icon="file" thickness="solid" />
            <p>Browse Image</p>
          </Button>
        </div>
      </div>
    {/snippet}
  </Overlay>
{/if}

<style lang="scss">
  @use '../../../../global.scss' as *;

  div.create-dialog {
    background-color: var(--color-9);
    color: var(--color-1);

    > div.header {
      flex-direction: row;

      align-items: center;

      > h2 {
        flex-grow: 1;

        padding: 0 8px;

        font-weight: bolder;
      }
    }

    > div.body {
      padding: 8px;
      gap: 8px;

      div.foreground {
        flex-grow: 1;
        flex-direction: row;
        align-items: center;
        justify-content: center;

        gap: 8px;

        background-color: var(--color-1);
        color: var(--color-5);

        padding: 8px;
      }
    }
  }

  div.file-browser {
    @include force-size(calc(100dvw - 64px), calc(100dvh - 64px));

    background-color: var(--color-9);
    color: var(--color-1);

    > div.header {
      flex-direction: row;

      align-items: center;

      > h2 {
        flex-grow: 1;

        padding: 0 8px;

        font-weight: bolder;
      }
    }

    > div.main {
      flex-grow: 1;
      min-height: 0;

      padding: 8px;
      gap: 16px;
    }
  }
</style>
