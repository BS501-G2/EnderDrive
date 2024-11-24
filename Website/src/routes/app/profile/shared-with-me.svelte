<script lang="ts">
  import { useServerContext, type UserResource } from '$lib/client/client'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import SharedEntry from './shared-entry.svelte'
  import UserTab from './user-tab.svelte'

  const { user }: { user: UserResource } = $props()
  const server = useServerContext()
</script>

{#await (async () => {
  const me = await server.me()
  const accesses = await server.getFileAccesses( { targetUserId: me.id === user.id ? undefined : me.id, authorUserId: me.id === user.id ? me.id : user.id } )

  return await Promise.all(accesses.map(async (fileAccess) => {
      const file = await server.getFile(fileAccess.fileId)

      return { file, fileAccess }
    }))
})()}
  <div class="loading">
    <LoadingSpinner size="3em" />
  </div>
{:then accesses}
  <div class="shared">
    {#each accesses as { file, fileAccess }}
      <SharedEntry {file} {fileAccess} />
    {/each}
  </div>
{/await}

<style lang="scss">
  div.loading {
    flex-grow: 1;

    align-items: center;

    justify-content: center;
  }
</style>
