<script lang="ts">
  import { PasswordResetRequestStatus, useServerContext } from '$lib/client/client'
  import { useAdminContext } from '$lib/client/contexts/admin'
  import { useAppContext } from '$lib/client/contexts/app'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import Table from '$lib/client/ui/table.svelte'
  import { onMount, type Snippet } from 'svelte'
  import ResetEntry from './reset-entry.svelte'
  import Separator from '$lib/client/ui/separator.svelte'
  import { writable } from 'svelte/store'

  const { getPasswordResetRequests } = useServerContext()
  const { isDesktop } = useAppContext()

  const { pushTitle } = useAdminContext()

  onMount(() => pushTitle('Password Reset Requests'))

  async function load() {
    return await getPasswordResetRequests({ status: PasswordResetRequestStatus.Pending })
  }

  const promise = writable<ReturnType<typeof load>>(load())
</script>

{#await $promise}
  <div class="loading">
    <LoadingSpinner size="64px" />
  </div>
{:then resets}
  <div class="list">
    {#each resets as reset, index}
      {#if index !== 0}
        <Separator horizontal />
      {/if}
      <ResetEntry
        {reset}
        refresh={() => {
          $promise = load()
        }}
      />
    {/each}
  </div>
{/await}

<style lang="scss">
  div.loading {
    flex-grow: 1;

    align-items: center;
    justify-content: center;
  }

  div.list {
    padding: 8px;
    gap: 8px;
  }
</style>
