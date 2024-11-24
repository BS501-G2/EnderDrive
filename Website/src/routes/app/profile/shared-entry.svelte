<script lang="ts">
  import { useServerContext, type FileAccessResource, type FileResource } from "$lib/client/client"
  import { useAppContext } from "$lib/client/contexts/app"
  import UserLink from "$lib/client/model/user-link.svelte"
  import LoadingSpinner from "$lib/client/ui/loading-spinner.svelte"
  import Separator from "$lib/client/ui/separator.svelte"
  import { toReadableSize } from "$lib/client/utils"
  import moment from "moment"
  import FileBrowserFileIcon from "../files/file-browser-file-icon.svelte"


  const { ...props }: { file: FileResource; fileAccess: FileAccessResource } | { head: true } = $props()
  const { isDesktop } = useAppContext()

  const server = useServerContext()
</script>

<div class="file-entry">
  <div class="icon">
    {#if 'head' in props}{:else}
      {#await server.getFileMime(props.file.id)}
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
      <a href="/app/files?fileId={props.file.id}">{props.file.name}</a>
    {/if}
  </div>

  {#if $isDesktop}
    <Separator vertical />
    <div class="size">
      {#if 'head' in props}
        <h2>Size</h2>
      {:else}
        {#await server.getFileSize(props.file.id)}
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
        {#await server.getFileLogs({ fileId: props.file.id, count: 1 })}
          <LoadingSpinner size="1rem" />
        {:then [{ actorUserId, createTime }]}
          <p>
            <b class="user"><UserLink userId={actorUserId} /></b>
            {moment(new Date(createTime).getTime()).fromNow()}
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
