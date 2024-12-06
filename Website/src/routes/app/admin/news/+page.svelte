<script lang="ts">
  import { useAdminContext } from '$lib/client/contexts/admin'
  import { onMount } from 'svelte'
  import CreateNewsButton from './news-create.svelte'
  import CreateNewsDialog from './create-news-dialog.svelte'
  import { writable, type Writable } from 'svelte/store'
  import EditNews from './edit-news.svelte'

  import NewsEntry from './news-entry.svelte'
  import { useClientContext } from '$lib/client/client'
  import { useAppContext } from '$lib/client/contexts/app'

  const { pushTitle, pushSidePanel } = useAdminContext()
  onMount(() => pushTitle('News'))

  const { isDesktop } = useAppContext()
  const { server } = useClientContext()

  const createDialog = writable<[newsId?: number] | null>(null)
  const editDialog: Writable<{ imageId: string; id: string | null } | null> = writable(null)

  let refreshKey = $state({})
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
      refreshKey={}
    }}
  />
{/if}

{#key refreshKey}
  {#await (async () => {
    const newsIds = await server.GetNews({})

    return { newsIds }
  })() then { newsIds }}
    <div class="page">
      {#if $isDesktop}
        <NewsEntry header />
      {/if}
      {#each newsIds as newsId}
        {#await server.GetNewsEntry({ NewsId: newsId }) then news}
          <NewsEntry
            {news}
            onrefresh={() => {
              refreshKey = {}
            }}
          />
        {/await}
      {/each}
    </div>
  {/await}
{/key}

<style lang="scss">
  div.page {
    padding: 8px;

    gap: 8px;
  }
</style>
