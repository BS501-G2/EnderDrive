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
  import { useClientContext } from '$lib/client/client'
  import { type UserResource } from '$lib/client/resource'
  import UserEntry from './user-entry.svelte'
  import LazyLoader from '$lib/client/ui/lazy-loader.svelte'

  const { pushSidePanel, pushTitle } = useAdminContext()
  const { searchString, includeRole, excludeRole } = createUserContext()
  const { server } = useClientContext()

  onMount(() => pushTitle('Teachers6656'))

  const createDialog = writable<boolean>(false)
  const createResult = writable<{ password: string; userId: string } | null>(null)
  let users: UserResource[] = $state([])
</script>

<UserEntry head />
<LazyLoader
  bind:items={users}
  load={async (offset) => {
    const users = await server.GetUsers({
      SearchString: $searchString,
      IncludeRole: $includeRole.length > 0 ? $includeRole : void 0,
      ExcludeRole: $excludeRole.length > 0 ? $excludeRole : void 0,
      Pagination: {
        Offset: offset,
        Count: 1
      }
    })

    return users
  }}
  vertical
>
  {#snippet itemSnippet(item, index, key)}
    <UserEntry user={item} />
  {/snippet}
</LazyLoader>

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
