<script lang="ts">
  import { useClientContext } from '$lib/client/client'
  import type { FileAccessResource } from '$lib/client/resource'
  import SharedGroup from './shared-group.svelte'

  const { server } = useClientContext()

  async function files() {
    const me = await server.Me({})
    const fileAccesses = await server.GetFileAccesses({
      TargetUserId: me.Id,
      Pagination: {
      },
      IncludePublic: false
    })

    const groupedFileAccesses: {
      userId: string
      fileAccesses: FileAccessResource[]
    }[] = []

    for (const fileAccess of fileAccesses) {
      const lastGroup = groupedFileAccesses[groupedFileAccesses.length - 1]

      if (lastGroup != null && lastGroup.userId == fileAccess.AuthorUserId) {
        lastGroup.fileAccesses.push(fileAccess)
      } else {
        groupedFileAccesses.push({ userId: fileAccess.AuthorUserId, fileAccesses: [fileAccess] })
      }
    }

    return groupedFileAccesses
  }
</script>

<div class="carousel-container">
  {#await files() then group}
    <div class="carousel">
      {#each group as { userId, fileAccesses }}
        <SharedGroup {userId} {fileAccesses} />
      {/each}
    </div>
  {/await}
</div>

<style lang="scss">
  div.carousel {
    flex-direction: row;

    gap: 16px;
    padding: 16px;
  }

  div.carousel-container {
    overflow: auto hidden;
  }
</style>
