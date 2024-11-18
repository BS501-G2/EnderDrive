<script lang="ts">
  import { useAdminContext } from '$lib/client/contexts/admin'
  import { onMount } from 'svelte'
  import CreateNewsButton from './create-news-button.svelte'
  import AdminSidePanel from '../admin-side-panel.svelte'
  import FilterNewsButton from './filter-news-button.svelte'
  import CreateNewsDialog from './create-news-dialog.svelte'
  import { writable, type Writable } from 'svelte/store'

  const { pushTitle, pushSidePanel } = useAdminContext()
  onMount(() => pushTitle('News'))

  let createDialog: [newsId?: number] | null = $state(null)
  const editDialog: Writable<[image: string, id?: string] | null> = writable(null)
</script>

<FilterNewsButton />

<CreateNewsButton
  onopen={() => {
    createDialog = []
  }}
/>

{#if createDialog != null}
  {@const [newsId] = createDialog}

  <CreateNewsDialog
    {newsId}
    onresult={(file) => {}}
    ondismiss={() => {
      createDialog = null
    }}
  />
{/if}

<style lang="scss">
</style>
