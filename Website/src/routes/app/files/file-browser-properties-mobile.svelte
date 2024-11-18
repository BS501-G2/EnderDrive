<script lang="ts">
  import { useFileBrowserContext } from '$lib/client/contexts/file-browser'
  import type { Snippet } from 'svelte'
  import Overlay from '../../overlay.svelte'
  import Separator from '$lib/client/ui/separator.svelte'
  import FileBrowserProperties from './file-browser-properties.svelte'

  const {
    selectedFileIds
  }: {
    selectedFileIds: string[]
  } = $props()
  const { showDetails } = useFileBrowserContext()
</script>

<Overlay
  ondismiss={() => {
    $showDetails = false
  }}
  nodim
>
  {#snippet children(windowButtons: Snippet)}
    <div class="mobile-properties">
      <div class="header">
        <div class="title">
          <p>Properties</p>
        </div>

        {@render windowButtons()}
      </div>

      <Separator horizontal />

      <div class="body">
        <FileBrowserProperties {selectedFileIds} />
      </div>
    </div>
  {/snippet}
</Overlay>

<style lang="scss">
  @use '../../../global.scss' as *;

  div.mobile-properties {
    @include force-size(100dvw, 100dvh);

    background-color: #0000008f;
    color: var(--color-9);

    > div.header {
      flex-direction: row;

      > div.title {
        align-items: center;
        flex-direction: row;

        padding: 16px;
        flex-grow: 1;
        font-size: 1.2em;
        font-weight: bolder;
      }
    }

    > div.body {
      flex-grow: 1;
    }
  }
</style>
