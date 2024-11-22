<script lang="ts">
  import { useServerContext, type UserResource } from '$lib/client/client'
  import { useAppContext } from '$lib/client/contexts/app'
  import UserLink from '$lib/client/model/user-link.svelte'
  import Button from '$lib/client/ui/button.svelte'
  import Icon, { type IconOptions } from '$lib/client/ui/icon.svelte'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import { type Snippet } from 'svelte'
  import { writable } from 'svelte/store'
  import ActionMenu from './action-menu.svelte'
  import RoleEditor from './role-editor.svelte'

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

      const oncontextmenu = (event: MouseEvent) => {
        if ($isMobile) {
          event.preventDefault()

          $showMenu = true
          return
        }
      }

      $rowElement.addEventListener('mouseenter', onmouseenter)
      $rowElement.addEventListener('mouseleave', onmouseleave)
      $rowElement.addEventListener('contextmenu', oncontextmenu)

      return () => {
        $rowElement.removeEventListener('mouseenter', onmouseenter)
        $rowElement.removeEventListener('mouseleave', onmouseleave)
        $rowElement.removeEventListener('contextmenu', oncontextmenu)
      }
    }
  })

  const menuButton = writable<HTMLButtonElement>(null as never)
  const showMenu = writable(false)
  const showEditRoles = writable(false)
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

      {#if ($hover || $showMenu) && $isDesktop}
        <div class="actions">
          {#snippet button(
            name: string,
            icon: IconOptions,
            onclick: (
              event: MouseEvent & { currentTarget: EventTarget & HTMLButtonElement }
            ) => Promise<void> | void
          )}
            {#snippet foreground(view: Snippet)}
              <div class="action-foreground">
                {@render view()}
              </div>
            {/snippet}

            <Button bind:buttonElement={$menuButton} {onclick} hint={name} {foreground}>
              <div class="action">
                <Icon {...icon} />
              </div>
            </Button>
          {/snippet}

          {@render button('Options', { icon: 'ellipsis-vertical', thickness: 'solid' }, () => {
            $showMenu = true
          })}
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

{#if $showMenu}
  <ActionMenu
    {user}
    {showEditRoles}
    menuButton={$menuButton}
    ondismiss={() => {
      $showMenu = false
    }}
  />
{/if}

{#if $showEditRoles}
  <RoleEditor
    {user}
    ondismiss={() => {
      $showEditRoles = false
    }}
  />
{/if}

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
          div.action-foreground {
            flex-grow: 1;

            padding: 8px;

            div.action {
              flex-direction: row;
              align-items: center;
            }
          }
        }
      }
    }

    td.roles {
      width: 128px;
    }

    p.username {
      font-style: italic;

      font-size: 0.8em;
    }
  }
</style>
