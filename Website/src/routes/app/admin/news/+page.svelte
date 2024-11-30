<script lang="ts">
  import { useAdminContext } from '$lib/client/contexts/admin'
  import { onMount } from 'svelte'
  import CreateNewsButton from './news-create.svelte'
  import CreateNewsDialog from './create-news-dialog.svelte'
  import { writable, type Writable } from 'svelte/store'
  import EditNews from './edit-news.svelte'

  import NewsEntry from './news-entry.svelte'
  import { useClientContext } from '$lib/client/client'

  const { pushTitle, pushSidePanel } = useAdminContext()
  onMount(() => pushTitle('News'))

  const { server } = useClientContext()

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
      $editDialog = { imageId: file.Id, id: null }

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
  const newsIds = await server.GetNews({})

  return { newsIds }
})() then { newsIds }}
  <div class="page">
    {#each newsIds as newsId}
      {#await server.getNewsEntry(newsId) then news}
        <NewsEntry {news} />
      {/await}
    {/each}
  </div>
{/await}
