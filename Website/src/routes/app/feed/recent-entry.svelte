<script lang="ts">
  import { useClientContext } from '$lib/client/client'
  import { type FileResource, type FileLogResource } from '$lib/client/resource'
  import { useAppContext } from '$lib/client/contexts/app'
  import UserLink from '$lib/client/model/user-link.svelte'
  import Icon from '$lib/client/ui/icon.svelte'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import Separator from '$lib/client/ui/separator.svelte'
  import moment from 'moment'
  import FileBrowserFileIcon from '../files/file-browser-file-icon.svelte'
  import { toReadableSize } from '$lib/client/utils'

  const { ...props }: { file: FileResource; fileLog: FileLogResource } | { head: true } = $props()
  const { isDesktop } = useAppContext()

  const { server } = useClientContext()
</script>

<div class="file-entry">
  <div class="icon">
    {#if 'head' in props}{:else}
      {#await (async () => {
        const mime = await server.FileGetMime({ FileId: props.file.Id })

        return mime
      })()}
        <LoadingSpinner size="2rem" />
      {:then mime}
        <FileBrowserFileIcon {mime} size="2rem" />
      {/await}
    {/if}
  </div>

  <Separator vertical />
  <div class="file-name">
    {#if 'head' in props}
      <h2>File Name</h2>
    {:else}
      <a href="/app/files?fileId={props.file.Id}">{props.file.Name}</a>
    {/if}
  </div>

  {#if $isDesktop}
    <Separator vertical />
    <div class="size">
      {#if 'head' in props}
        <h2>Size</h2>
      {:else}
        {#await server.FileGetSize({ FileId: props.file.Id })}
          <LoadingSpinner size="1rem" />
        {:then size}
          <p>
            {toReadableSize(size)}
          </p>
        {/await}
      {/if}
    </div>
    <Separator vertical />
    <div class="by">
      {#if 'head' in props}
        <h2>Modified</h2>
      {:else}
        {#await server.GetFileLogs({ FileId: props.file.Id, Pagination: { Count: 1 }, UniqueFileId: true })}
          <LoadingSpinner size="1rem" />
        {:then [{ ActorUserId, CreateTime }]}
          <p>
            <b class="user"><UserLink userId={ActorUserId} /></b>
            {moment(CreateTime).fromNow()}
          </p>
        {/await}
      {/if}
    </div>
  {/if}
</div>

<style lang="scss">
  @use '../../../global.scss' as *;

  div.file-entry {
    flex-direction: row;
    align-items: center;

    gap: 8px;
    padding: 0 8px;

    h2 {
      font-weight: bolder;
    }

    > div.icon {
      @include force-size(2rem, 2rem);
      padding: 8px 0;
    }

    > div.file-name {
      flex-grow: 1;

      > a {
        color: inherit;
        text-decoration: none;
      }

      > a:hover {
        text-decoration: underline;
      }
    }

    > div.size {
      @include force-size(96px, &);

      text-align: end;
    }

    > div.by {
      @include force-size(256px, &);

      b.user {
        font-weight: bolder;
      }
    }
  }

  div.file-entry:hover {
    background-color: var(--color-5);
  }
</style>
