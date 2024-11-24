<script lang="ts">
  import { useServerContext } from '$lib/client/client'
  import type { FileProperties } from '$lib/client/contexts/file-browser'
  import { derived } from 'svelte/store'
  import FileBrowserPropertiesTab from './file-browser-properties-tab.svelte'
  import { page } from '$app/stores'
  import moment from 'moment'
  import UserLink from '$lib/client/model/user-link.svelte'
  const { file }: { file: FileProperties } = $props()

  const server = useServerContext()
</script>

<FileBrowserPropertiesTab label="File Revisions" icon={{ icon: 'history', thickness: 'solid' }}>
  {#await (async () => {
    const fileContent = await server.getMainFileContent(file.file.id)
    const fileSnapshots = await server.getFileSnapshots(file.file.id, fileContent.id)

    return { fileSnapshots }
  })() then { fileSnapshots }}
    <div class="list">
      {#each fileSnapshots as fileSnapshot}
      {#if fileSnapshot.size > 0}
      <p>
        <a href="/app/files?fileId={file.file.id}&snapshotId={fileSnapshot.id}"
          >{moment(new Date(fileSnapshot.createTime))}</a
        >
        by <UserLink userId={fileSnapshot.authorUserId} />
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
