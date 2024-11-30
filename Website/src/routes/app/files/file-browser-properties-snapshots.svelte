<script lang="ts">
  import { useClientContext } from '$lib/client/client'
  import type { FileProperties } from '$lib/client/contexts/file-browser'
  import { derived } from 'svelte/store'
  import FileBrowserPropertiesTab from './file-browser-properties-tab.svelte'
  import { page } from '$app/stores'
  import moment from 'moment'
  import UserLink from '$lib/client/model/user-link.svelte'
  const { file }: { file: FileProperties } = $props()

  const { server } = useClientContext()
</script>

<FileBrowserPropertiesTab label="File Revisions" icon={{ icon: 'history', thickness: 'solid' }}>
  {#await (async () => {
    const fileDataEntries = await server
      .FileGetDataEntries({ FileId: file.file.Id })
      .then((fileEntries) => Promise.all(fileEntries.map(async (fileData) => {
            const size = await server.FileDataGetSize( { FileId: file.file.Id, FileDataId: fileData.Id } )

            return { fileData, size }
          })))

    return { fileDataEntries }
  })() then { fileDataEntries }}
    <div class="list">
      {#each fileDataEntries as { fileData, size }}
        {#if size > 0}
          <p>
            Revision
            <a href="/app/files?fileId={file.file.Id}&snapshotId={fileData.Id}">
              #{fileData.Id.slice(fileData.Id.length - 4, fileData.Id.length)}
            </a>
            by <UserLink userId={fileData.AuthorUserId!} />
            {moment(new Date(fileData.CreateTime)).fromNow()}
          </p>
        {/if}
      {/each}
    </div>
  {/await}
</FileBrowserPropertiesTab>

<style lang="scss">
  div.list {
    padding: 8px;
    gap: 8px;

    a {
      color: inherit;
    }
  }
</style>
