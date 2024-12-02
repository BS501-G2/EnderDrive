<script lang="ts">
  import Window from '$lib/client/ui/window.svelte'
  import { writable } from 'svelte/store'
  import { onMount, type Snippet } from 'svelte'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import Input from '$lib/client/ui/input.svelte'
  import Button from '$lib/client/ui/button.svelte'
  import { useClientContext } from '$lib/client/client'

  const { server } = useClientContext()

  const { id, imageId, ondismiss }: { id: string | null; imageId: string; ondismiss: () => void } =
    $props()

  const imageUrl = writable<string | null>(null)

  onMount(() => {
    void (async () => {
      const fileData = await server
        .FileGetDataEntries({ FileId: imageId, Pagination: { Count: 1 } })
        .then((result) => result[0])
      const size = await server.FileGetSize({ FileId: imageId, FileDataId: fileData.Id })

      let data = new Blob()
      const streamId = await server.StreamOpen({
        FileId: imageId,
        FileDataId: fileData.Id,
        ForWriting: true
      })
      const bufferSize = 1024 * 8

      for (let index = 0; index < size; index += bufferSize) {
        const buffer = await server.StreamRead({ StreamId: streamId, Length: bufferSize })

        data = new Blob([data, buffer])
      }

      $imageUrl = URL.createObjectURL(data)
    })()
  })

  const value = writable<string>('')
</script>

<Window title="{id == null ? 'Create' : 'Edit'} news" {ondismiss}>
  <div class="edit-news">
    {#if $imageUrl == null}
      <div class="loading">
        <LoadingSpinner size="3rem" />
      </div>
    {:else}
      <img src={$imageUrl} alt="test" />

      <Input type="text" id="id" bind:value={$value} name="News Title" />

      {#snippet foreground(view: Snippet)}
        <div class="foreground">
          {@render view()}
        </div>
      {/snippet}

      {#snippet background(view: Snippet)}
        <div class="background">
          {@render view()}
        </div>
      {/snippet}

      <Button
        {foreground}
        {background}
        onclick={async () => {
          await server.CreateNews({
            Title: $value,
            ImageFileId: imageId,
            PublishTime: Date.now()
          })
          ondismiss()
        }}
      >
        <p>Save</p>
      </Button>
    {/if}
  </div>
</Window>

<style lang="scss">
  @use '../../../../global.scss' as *;

  div.background {
    background-color: var(--color-1);
    color: var(--color-5);
  }

  div.foreground {
    padding: 8px;
  }

  div.edit-news {
    img {
      @include force-size(min(640px, 75dvw), min(360px, 75dvh));

      object-fit: contain;
    }

    gap: 8px;
    overflow: hidden auto;
  }

  div.loading {
    flex-grow: 1;

    align-items: center;
    justify-content: center;
  }
</style>
