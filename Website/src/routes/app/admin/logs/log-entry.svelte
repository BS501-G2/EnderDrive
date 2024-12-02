<script lang="ts">
  import { useClientContext } from '$lib/client/client'
  import UserLink from '$lib/client/model/user-link.svelte'
  import { FileLogType, FileType, type FileLogResource } from '$lib/client/resource'
  import Icon from '$lib/client/ui/icon.svelte'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import Separator from '$lib/client/ui/separator.svelte'
  import FileBrowserFileIcon from '../../files/file-browser-file-icon.svelte'

  const { fileLog }: { fileLog: FileLogResource } = $props()

  const { server } = useClientContext()
</script>

<div class="user">
  <div class="icon">
    <Icon icon="pencil" thickness="solid" size="1.2rem" />
  </div>

  <div class="name">
    <p class="message">
      <b class="user"><UserLink userId={fileLog.ActorUserId} /></b> performed {FileLogType[
        fileLog.Type
      ]} operation on a file
    </p>

    <div class="file">
      {#await (async () => {
        const file = await server.GetFile({ FileId: fileLog.FileId })

        if (file.Type === FileType.Folder) {
          return { file }
        }

        const fileData = (await server.FileGetDataEntries( { FileId: file.Id, Pagination: { Count: 1 } } ))[0]
        const mime = await server.FileGetMime({ FileId: file.Id, FileDataId: fileData.Id })

        return { file, mime }
      })()}
        <LoadingSpinner size="1rem" />
      {:then { file, mime }}
        <FileBrowserFileIcon size="1rem" {mime} type={file.Type} />
        <a class="file" href="/app/files?fileId={file.Id}">{file.Name}</a>
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
