<script lang="ts">
  import { useAdminContext } from '$lib/client/contexts/admin'
  import { onMount } from 'svelte'
  import CreateNewsButton from './news-create.svelte'
  import CreateNewsDialog from './create-news-dialog.svelte'
  import { writable, type Writable } from 'svelte/store'
  import EditNews from './edit-news.svelte'
  import { useServerContext } from '$lib/client/client'
  import NewsEntry from './news-entry.svelte'

  const { pushTitle, pushSidePanel } = useAdminContext()
  onMount(() => pushTitle('News'))

  const server = useServerContext()

  const createDialog = writable<[newsId?: number] | null>(null)
  const editDialog: Writable<{ imageId: string; id: string | null } | null> = writable(null)
</script>


<CreateNewsButton
  onopen={() => {
    $createDialog = []
  }}
/>

{#if $createDialog != null}
  {@const [newsId] = $createDialog}

  <CreateNewsDialog
    {newsId}
    onresult={(file) => {
      $editDialog = { imageId: file.id, id: null }

      $createDialog = null
    }}
    oncancel={() => {
      $createDialog = null
    }}
  />
{/if}

{#if $editDialog != null}
  <EditNews
    {...$editDialog}
    ondismiss={() => {
      $editDialog = null
    }}
  />
{/if}

{#await (async () => {
  const newsEntries = await server.getNews()

  return { newsEntries }
})() then { newsEntries }}
  <div class="page">
    {#each newsEntries as newsId}
      {#await server.getNewsEntry(newsId) then news}
        <NewsEntry {news} />
      {/await}
    {/each}
  </div>
{/await}
