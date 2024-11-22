<script lang="ts">
  import { useAdminContext } from '$lib/client/contexts/admin'
  import { onMount, type Snippet } from 'svelte'
  import AdminSidePanel from '../admin-side-panel.svelte'
  import Input from '$lib/client/ui/input.svelte'
  import { createUserContext } from './user-context'
  import Button from '$lib/client/ui/button.svelte'
  import { writable } from 'svelte/store'
  import Window from '$lib/client/ui/window.svelte'
  import CreateUserDialog from './create-user-dialog.svelte'
  import { UserRole, useServerContext } from '$lib/client/client'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import UserTable from './user-table.svelte'

  const { pushSidePanel, pushTitle } = useAdminContext()
  const { searchString, includeRole, excludeRole } = createUserContext()
  const { getUsers } = useServerContext()

  onMount(() => pushTitle('Users'))

  async function load(
    searchString: string,
    includeRole: UserRole[] | null,
    excludeRole: UserRole[] | null
  ) {
    const users = await getUsers({
      searchString,
      includeRole: includeRole ?? void 0,
      excludeRole: excludeRole ?? void 0,
      excludeSelf: false
    })

    return users
  }

  const createDialog = writable<boolean>(false)
  const createResult = writable<{ password: string; userId: string } | null>(null)
  const users = writable<ReturnType<typeof load> | null>(null)

  $effect(() => {
    $users = load(
      $searchString,
      $includeRole.length > 0 ? $includeRole : [],
      $excludeRole.length > 0 ? $excludeRole : []
    )
  })
</script>

{#await $users}
  <div class="loading">
    <LoadingSpinner size="3rem" />
  </div>
{:then users}
  <div class="page">
    <UserTable users={users || []} />
  </div>
{:catch error}
  {#each error as a}
    <p class="error">{a}</p>
  {/each}
{/await}

{#snippet foreground(view: Snippet)}
  <div class="button-foreground">
    {@render view()}
  </div>
{/snippet}

<AdminSidePanel icon={{ icon: 'user' }} name="Filter">
  <div class="panel">
    <Input id="search" type="text" placeholder="Search..." bind:value={$searchString} />
  </div>
</AdminSidePanel>

<AdminSidePanel icon={{ icon: 'user' }} name="Create">
  <div class="panel">
    <p>Create a new user to add to the system.</p>

    <Button
      {foreground}
      onclick={() => {
        $createDialog = true
      }}
    >
      <p>Create User</p>
    </Button>
  </div>
</AdminSidePanel>

{#if $createDialog}
  <CreateUserDialog
    ondismiss={() => {
      $createDialog = false
    }}
    onresult={({ password, userId }) => {
      $createResult = { password, userId }

      $createDialog = false
    }}
  />
{/if}

{#if $createResult != null}
  {@const { password, userId } = $createResult ?? {}}

  <Window
    ondismiss={() => {
      $createResult = null
    }}
  >
    <div class="create-dialog">
      <p>Created user with id {userId} and password {password}</p>
    </div>
  </Window>
{/if}

<style lang="scss">
  @use '../../../../global.scss' as *;

  div.button-foreground {
    flex-grow: 1;

    padding: 8px;

    background-color: var(--color-1);
    color: var(--color-9);
  }

  div.panel {
    gap: 8px;
  }

  div.create-dialog {
  }

  div.loading {
    flex-grow: 1;

    align-items: center;
    justify-content: center;
  }

  div.page {
    flex-grow: 1;
    padding: 0 8px;

    box-sizing: border-box;
    min-height: 0;
  }
</style>
