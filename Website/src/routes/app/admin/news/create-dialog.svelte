<script lang="ts">
  import type { Snippet } from 'svelte'
  import Overlay from '../../../overlay.svelte'
  import Button from '$lib/client/ui/button.svelte'
  import Icon from '$lib/client/ui/icon.svelte'
  import { writable, type Writable } from 'svelte/store'

  const {
    ondismiss,
    browse
  }: { ondismiss: () => void; browse: Writable<[fileId: string | null] | null> } = $props()
</script>

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
            $browse = [null]
          }}
        >
          <Icon icon="file" thickness="solid" />
          <p>Browse Image</p>
        </Button>
      </div>
    </div>
  {/snippet}
</Overlay>

<style lang="scss">
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
</style>
