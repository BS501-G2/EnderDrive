<script lang="ts">
  import { useServerContext, type FileAccessResource } from '$lib/client/client'
  import SharedGroup from './shared-group.svelte'

  const server = useServerContext()

  async function files() {
    const me = await server.me()
    const fileAccesses = await server.getFileAccesses({
      targetUserId: me.id,
      count: 50
    })

    const groupedFileAccesses: {
      userId: string
      fileAccesses: FileAccessResource[]
    }[] = []

    for (const fileAccess of fileAccesses) {
      const lastGroup = groupedFileAccesses[groupedFileAccesses.length - 1]

      if (lastGroup != null && lastGroup.userId == fileAccess.authorUserId) {
        lastGroup.fileAccesses.push(fileAccess)
      } else {
        groupedFileAccesses.push({ userId: fileAccess.authorUserId, fileAccesses: [fileAccess] })
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
