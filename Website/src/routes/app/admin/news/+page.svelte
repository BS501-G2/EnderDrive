<script lang="ts">
  import { useAdminContext } from '$lib/client/contexts/admin'
  import { onMount } from 'svelte'
  import CreateNewsButton from './news-create.svelte'
  import AdminSidePanel from '../admin-side-panel.svelte'
  import FilterNewsButton from './news-filter.svelte'
  import CreateNewsDialog from './create-news-dialog.svelte'
  import { writable, type Writable } from 'svelte/store'
  import EditNews from './edit-news.svelte'

  const { pushTitle, pushSidePanel } = useAdminContext()
  onMount(() => pushTitle('News'))

  const createDialog = writable<[newsId?: number] | null>(null)
  const editDialog: Writable<{ imageId: string; id: string | null } | null> = writable(null)
</script>

<FilterNewsButton />

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
  <EditNews {...$editDialog} />
{/if}

<style lang="scss">
</style>
