<script lang="ts">
  import { useServerContext, type UserResource } from '$lib/client/client'
  import { useAppContext } from '$lib/client/contexts/app'
  import UserLink from '$lib/client/model/user-link.svelte'
  import Icon from '$lib/client/ui/icon.svelte'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import { type Snippet } from 'svelte'
  import { writable } from 'svelte/store'

  const { user }: { user: UserResource } = $props()
  const { amIAdmin, isUserAdmin } = useServerContext()
  const { isDesktop, isMobile } = useAppContext()

  const hover = writable(false)
  const rowElement = writable<HTMLTableRowElement>(null as never)

  $effect(() => {
    if ($rowElement) {
      const onmouseenter = () => {
        hover.set(true)
      }

      const onmouseleave = () => {
        hover.set(false)
      }

      $rowElement.addEventListener('mouseenter', onmouseenter)
      $rowElement.addEventListener('mouseleave', onmouseleave)

      return () => {
        $rowElement.removeEventListener('mouseenter', onmouseenter)
        $rowElement.removeEventListener('mouseleave', onmouseleave)
      }
    }
  })
</script>

<tr class="user" bind:this={$rowElement} class:hover={$hover}>
  <td class="icon">
    <div class="icon">
      <!-- <input type="checkbox" checked={true} /> -->
      <Icon icon="user-circle" size="2em" />
    </div>
  </td>
  <td class="main">
    <div class="main">
      <div class="user-name">
        <UserLink userId={user.id} />
        <p class="username">@{user.username}</p>
      </div>

      {#if $hover}
        <div class="actions">
          {#snippet foreground(view: Snippet)}
            <div class="foreground">
              {@render view()}
            </div>
          {/snippet}
          action
        </div>
      {/if}
    </div>
  </td>

  {#if $isDesktop}
    <td class="roles">
      {#each user.roles as role, index (role)}
        {#if index !== 0},{/if}{role}
      {/each}
    </td>

    <td class="admin">
      {#await isUserAdmin(user.id)}
        <LoadingSpinner size="1rem" />
      {:then isAdmin}
        {#if isAdmin}
          <p>Yes</p>
        {:else}
          <p>No</p>
        {/if}
      {/await}
    </td>
  {/if}
</tr>

<style lang="scss">
  tr.user {
    td.icon {
      width: 16px;
      justify-items: stretch;

      div.icon {
        flex-direction: row;

        > input {
          opacity: 0;
        }

        > input:hover {
          opacity: 1;
        }
      }
    }

    td.admin {
      width: 128px;
    }

    td.main {
      justify-items: stretch;

      div.main {
        flex-direction: row;

        > div.user-name {
          flex-grow: 1;
        }

        > div.actions {
        }
      }
    }

    td.roles {
      width: 256px;
    }

    p.username {
      font-style: italic;

      font-size: 0.8em;
    }
  }
</style>
