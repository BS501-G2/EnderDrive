<script lang="ts">
  import { useServerContext } from '$lib/client/client'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import { writable } from 'svelte/store'

  const {
    fileId
  }: {
    fileId: string
  } = $props()
  const { getFileContents, getFileSnapshots, getFile, scanFile, getFileMime } =
    useServerContext()

  async function load() {
    const file = await getFile(fileId)
    const mime = await getFileMime(fileId)
    const fileContent = (await getFileContents(fileId, 0, 1))[0]
    const fileSnapshot = (
      await getFileSnapshots(fileId, fileContent.id, 0, 1)
    )[0]
    const virusResult = await scanFile(fileId, fileContent.id, fileSnapshot.id)

    return {
      file,
      mime,
      fileContent,
      fileSnapshot,
      virusResult
    }
  }

  let promise = writable(load())
</script>

{#snippet loading()}
  <div class="loading">
    <LoadingSpinner size="3em" />
  </div>
{/snippet}

{#await $promise}
  {@render loading()}
{:then { file, mime, fileContent, fileSnapshot, virusResult }}
  {#if virusResult.viruses.length > 0}
    {#each virusResult.viruses as a}
      <p>{a}</p>
    {/each}
  {/if}
{/await}

<style lang="scss">
  div.loading {
    flex-grow: 1;

    align-items: center;
    justify-content: center;
  }
</style>
