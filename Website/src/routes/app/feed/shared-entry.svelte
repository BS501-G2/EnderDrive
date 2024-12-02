<script lang="ts">
  import { goto } from '$app/navigation'
  import { useClientContext } from '$lib/client/client'
  import Button from '$lib/client/ui/button.svelte'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import { type Snippet } from 'svelte'
  import FileBrowserFileIcon from '../files/file-browser-file-icon.svelte'
  import type { FileAccessResource } from '$lib/client/resource'

  const { fileAccess }: { fileAccess: FileAccessResource } = $props()
  const { server } = useClientContext()
</script>

{#snippet background(view: Snippet)}
  <div class="background">
    {@render view()}
  </div>
{/snippet}

{#snippet foreground(view: Snippet)}
  <div class="foreground">
    {@render view()}
  </div>
{/snippet}

<Button {foreground} {background} onclick={() => goto(`/app/files?fileId=${fileAccess.FileId}`)}>
  {#await Promise.all( [server.FileGetMime( { FileId: fileAccess.FileId } ), server.GetFile( { FileId: fileAccess.FileId } )] )}
    <div class="loading">
      <LoadingSpinner size="3rem" />
    </div>
  {:then [mime, file]}
    <div class="icon">
      <FileBrowserFileIcon {mime} size="3rem" />
    </div>
    <p class="name">{file.Name}</p>
  {/await}
</Button>

<style lang="scss">
  @use '../../../global.scss' as *;

  div.background {
    background-color: var(--color-9);
    color: var(--color-1);

    @include force-size(144px, 168px);
    align-items: center;
    justify-content: center;
  }

  div.foreground {
    padding: 8px;

    justify-content: center;
    align-items: center;

    border-radius: 8px;

    @include force-size(calc(128px), &);

    div.loading {
      align-items: center;
      justify-content: center;
    }

    div.icon {
      flex-grow: 1;

      align-items: center;
      justify-content: center;

      @include force-size(128px, 128px);

      border: solid 1px;
      border-color: inherit;
    }

    p.name {
      overflow: hidden;
      text-overflow: ellipsis;
      text-wrap: nowrap;

      @include force-size(calc(128px), &);
    }
  }
</style>
