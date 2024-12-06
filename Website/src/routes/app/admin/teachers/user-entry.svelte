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
</script>

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
        <a href="/profile?id={props.user.Id}">
          {props.user.FirstName}{props.user.MiddleName && ` ${props.user.MiddleName}`}
          {props.user.LastName}
        </a>
      </p>
    {/if}
  </div>

  {#if !('head' in props)}
    <div class="actions">
      <Button
        bind:buttonElement={$menuButton}
        onclick={() => {
          showMenu = true
        }}
      >
        <Icon icon="ellipsis-vertical" thickness="solid" />
      </Button>
    </div>
  {/if}
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

    div.icon {
      @include force-size(32px, &);
    }

    div.name {
      flex-grow: 1;

      > b {
        font-weight: bolder;
      }

      > p {
        > a {
          text-decoration: none;
          color: var(--color-1);
        }

        > a:hover {
          text-decoration: underline;
        }
      }
    }
  }
</style>
