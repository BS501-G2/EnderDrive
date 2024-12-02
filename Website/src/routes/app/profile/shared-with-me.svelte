<script lang="ts">
  import { useClientContext } from '$lib/client/client'
  import type { UserResource } from '$lib/client/resource'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import SharedEntry from './shared-entry.svelte'

  const { user }: { user: UserResource } = $props()
  const { server } = useClientContext()
</script>

{#await (async () => {
  const me = await server.Me({})
  const accesses = await server.GetFileAccesses( { TargetUserId: me.Id === user.Id ? undefined : me.Id, AuthorUserId: me.Id === user.Id ? me.Id : user.Id, IncludePublic: false } )

  return await Promise.all(accesses.map(async (fileAccess) => {
      const file = await server.GetFile({ FileId: fileAccess.FileId })

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
