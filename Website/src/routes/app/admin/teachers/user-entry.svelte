<script lang="ts">
  import { useClientContext } from '$lib/client/client'
  import { UserRole, type UserResource } from '$lib/client/resource'
  import { useAppContext } from '$lib/client/contexts/app'
  import { writable } from 'svelte/store'
  import RoleEditor from './role-editor.svelte'
  import Icon from '$lib/client/ui/icon.svelte'
  import UserLink from '$lib/client/model/user-link.svelte'
  import ActionMenu from './action-menu.svelte'
  import Button from '$lib/client/ui/button.svelte'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import { toReadableSize } from '$lib/client/utils'
  import { type Snippet } from 'svelte'
  import DataUsageDialog from '../../data-usage-dialog.svelte'

  const { ...props }: { user: UserResource } | { head: true } = $props()
  const { server } = useClientContext()
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

          showMenu = true
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
  let showMenu = $state(false)

  let roleEditor = $state<{ user: UserRole; roles: UserRole[] }>()

  let showSize = $state(false)
</script>

{#if showSize && !('head' in props)}
  <DataUsageDialog
    user={props.user}
    ondismiss={() => {
      showSize = false
    }}
  />
{/if}

<div class="user">
  <div class="icon">
    {#if !('head' in props)}
      <Icon icon="user" size="2rem" />
    {/if}
  </div>

  <div class="name">
    {#if 'head' in props}
      <b>Name</b>
    {:else}
      <p>
        <a href="/app/profile?id={props.user.Id}">
          {props.user.FirstName}{props.user.MiddleName && ` ${props.user.MiddleName}`}
          {props.user.LastName}
        </a>
      </p>
      <p class="username">@{props.user.Username}</p>
    {/if}
  </div>

  <div class="size">
    {#if 'head' in props}
      <b>Data Usage</b>
    {:else}
      {#await server.GetUserDiskUsage({ UserId: props.user.Id })}
        <LoadingSpinner size="1rem" />
      {:then a}
        {#snippet foreground(view: Snippet)}
          <div class="foreground">
            {@render view()}
          </div>
        {/snippet}
        <Button
          {foreground}
          onclick={() => {
            showSize = true
          }}
        >
          <p>{toReadableSize(a.DiskUsage)}</p>
        </Button>
      {/await}
    {/if}
  </div>
  <div class="actions">
    {#if !('head' in props)}
      {#snippet foreground(view: Snippet)}
        <div class="foreground">
          {@render view()}
        </div>
      {/snippet}

      <Button
        {foreground}
        bind:buttonElement={$menuButton}
        onclick={() => {
          showMenu = true
        }}
      >
        <Icon icon="ellipsis-vertical" thickness="solid" />
      </Button>
    {/if}
  </div>
</div>

{#if 'head' in props}{:else if showMenu}
  <ActionMenu
    user={props.user}
    menuButton={$menuButton}
    ondismiss={() => {
      showMenu = false
    }}
  />
{/if}

{#if roleEditor != null}
  {@const { user, roles } = $state.snapshot(roleEditor)}
{/if}

<style lang="scss">
  @use '../../../../global.scss' as *;

  div.user {
    padding: 8px;
    gap: 8px;

    flex-direction: row;
    align-items: center;

    b {
      font-weight: bolder;
    }

    div.icon {
      @include force-size(32px, &);
    }

    div.name {
      flex-grow: 1;

      > p {
        > a {
          text-decoration: none;
          color: var(--color-1);
        }

        > a:hover {
          text-decoration: underline;
        }
      }

      > p.username {
        font-size: 80%;
        font-weight: lighter;
      }
    }

    div.size {
      @include force-size(96px, &);

      justify-content: center;
      text-align: center;

      div.foreground {
        flex-grow: 1;
        align-items: end;
        padding: 8px;
      }
    }

    div.actions {
      @include force-size(32px, &);

      div.foreground {
        padding: 8px;
      }
    }
  }
</style>
