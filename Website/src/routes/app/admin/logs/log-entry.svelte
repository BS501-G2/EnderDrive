<script lang="ts">
  import { FileLogType, useServerContext, type FileLogResource } from '$lib/client/client'
  import UserLink from '$lib/client/model/user-link.svelte'
  import Icon from '$lib/client/ui/icon.svelte'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import Separator from '$lib/client/ui/separator.svelte'
  import FileBrowserFileIcon from '../../files/file-browser-file-icon.svelte'

  const { fileLog }: { fileLog: FileLogResource } = $props()

  const server = useServerContext()
</script>

<div class="user">
  <div class="icon">
    <Icon icon="pencil" thickness="solid" size="1.2rem" />
  </div>

  <div class="name">
    <p class="message">
      <b class="user"><UserLink userId={fileLog.actorUserId} /></b> performed {FileLogType[
        fileLog.type
      ]} operation on a file
    </p>

    <div class="file">
      {#await (async () => {
        const file = await server.getFile(fileLog.fileId)
        const mime = await server.getFileMime(file.id)

        return { file, mime }
      })()}
        <LoadingSpinner size="1rem" />
      {:then { file, mime }}
        <FileBrowserFileIcon size="1rem" {mime} type={file.type} />
        <a class="file" href="/app/files?fileId={file.id}">{file.name}</a>
      {/await}
    </div>
  </div>
</div>

<style lang="scss">
  div.user {
    flex-direction: row;

    gap: 16px;

    div.name {
      flex-grow: 1;

      gap: 8px;

      div.file {
        flex-direction: row;

        padding: 8px;
        gap: 8px;

        background-color: var(--color-5);

        a.file {
          color: inherit;
          text-decoration: none;
        }

        a.file:hover {
          text-decoration: underline;
        }
      }
    }
  }
</style>
