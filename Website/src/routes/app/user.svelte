<script lang="ts">
  import { useServerContext, type UserResource } from '$lib/client/client'
  import { useDashboardContext } from '$lib/client/contexts/dashboard'
  import Button from '$lib/client/ui/button.svelte'
  import Icon from '$lib/client/ui/icon.svelte'
  import { type Snippet } from 'svelte'
  import Overlay from '../overlay.svelte'
  import UserMenu from './user-menu.svelte'
  import { goto } from '$app/navigation'
  import Separator from '$lib/client/ui/separator.svelte'
  import { fly } from 'svelte/transition'

  const { pushDesktopTopRight } = useDashboardContext()

  let {
    user,
    logoutConfirmation = $bindable()
  }: {
    user: UserResource
    logoutConfirmation: boolean
  } = $props()

  $effect(() => pushDesktopTopRight(desktop))

  let showUserMenu: boolean = $state(false)
  let buttonElement: HTMLDivElement = $state(null as never)
</script>

{#snippet desktop()}
  {#snippet background(view: Snippet)}
    <div class="background" bind:this={buttonElement}>
      {@render view()}
    </div>
  {/snippet}

  {#snippet foreground(view: Snippet)}
    <div class="foreground">
      {@render view()}
    </div>
  {/snippet}

  <div class="user-button">
    <Button
      {background}
      {foreground}
      onclick={() => {
        showUserMenu = true
      }}
    >
      <Icon icon="user-circle" size="2rem" />
      <Icon icon="chevron-down" thickness="solid" />
      <!-- <p>{user!.firstName}</p> -->
    </Button>
  </div>
{/snippet}

{#if showUserMenu}
  <Overlay
    ondismiss={() => {
      showUserMenu = false
    }}
    nodim
    notransition
    x={-(
      1 +
      window.innerWidth -
      (buttonElement.getBoundingClientRect().x +
        buttonElement.getBoundingClientRect().width)
    )}
    y={buttonElement.getBoundingClientRect().y +
      buttonElement.getBoundingClientRect().height}
  >
    <div
      class="user-menu"
      transition:fly|global={{
        y: -16
      }}
    >
      {#snippet foreground(view: Snippet)}
        <div class="foreground">
          {@render view()}
        </div>
      {/snippet}
      <Button
        {foreground}
        onclick={() => {
          goto(`/app/profile?id=${user.id}`)
          showUserMenu = false
        }}
        hint="Go to your profile"
      >
        <div class="details">
          <div class="avatar">
            <Icon icon="user-circle" size="3rem" />
          </div>
          <div class="info">
            <h2 class="name">
              {user.firstName}
            </h2>
            <p>
              @{user.username}
            </p>
          </div>
        </div>
      </Button>

      <Separator horizontal />

      <UserMenu
        {user}
        bind:logoutConfirmation
        ondismiss={() => {
          showUserMenu = false
        }}
      />
    </div>
  </Overlay>
{/if}

<style lang="scss">
  div.background {
    border-radius: 8px;
    overflow: hidden;
  }

  div.foreground {
    padding: 4px;

    flex-direction: row;
    align-items: center;

    gap: 8px;
  }

  div.user-button {
    flex-direction: row;
    align-items: center;
  }

  div.user-menu {
    background-color: var(--color-9);
    color: var(--color-1);

    border-radius: 8px;

    padding: 8px;
    gap: 8px;

    div.details {
      flex-direction: row;
      align-items: center;

      padding: 8px;
      gap: 8px;

      > div.info {
        text-align: start;

        h2.name {
          font-size: 1.2rem;
          font-weight: bolder;
        }
      }
    }

    div.details:hover {
      > div.info {
        text-decoration: underline;
      }
    }
  }
</style>
