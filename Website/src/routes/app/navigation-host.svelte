<script lang="ts">
  import { goto } from '$app/navigation'
  import { page } from '$app/stores'
  import { useAppContext } from '$lib/client/contexts/app'
  import { type NavigationEntry } from '$lib/client/contexts/navigation'
  import { derived, type Readable } from 'svelte/store'
  import Navigation from './navigation.svelte'
  import {
    useClientContext,
    UserRole,
    useServerContext
  } from '$lib/client/client'

  const { isMobile } = useAppContext()
  const { clientState } = useClientContext()
  const { me, getUser, amIAdmin } = useServerContext()
  const {
    navigationEntries
  }: {
    navigationEntries: Readable<NavigationEntry[]>
  } = $props()
</script>

<div class="navigation" class:mobile={$isMobile}>
  {#each $navigationEntries as { id, snippet } (id)}
    {@render snippet()}
  {/each}
</div>

<Navigation
  label="Feed"
  onclick={async () => goto('/app/feed')}
  icon={(isActive) => ({
    icon: 'newspaper',
    thickness: isActive ? 'solid' : 'regular'
  })}
  isActive={derived(page, ({ url: { pathname } }) => {
    if (pathname === '/app/feed') {
      return true
    }

    return false
  })}
/>

<Navigation
  label="Files"
  onclick={async () => goto('/app/files' + ($isMobile ? '-mobile' : ''))}
  icon={(isActive) => ({
    icon: 'folder-open',
    thickness: isActive ? 'solid' : 'regular'
  })}
  isActive={derived(
    [page, isMobile],
    ([
      {
        url: { pathname }
      },
      isMobile
    ]) => {
      return isMobile
        ? pathname === '/app/files-mobile' ||
            pathname === '/app/files' ||
            pathname === '/app/shared' ||
            pathname === '/app/starred' ||
            pathname === '/app/trash'
        : pathname === '/app/files'
    }
  )}
/>

{#if !$isMobile}
  <Navigation
    label="Starred"
    onclick={async () => goto('/app/starred')}
    icon={(isActive) => ({
      icon: 'star',
      thickness: isActive ? 'solid' : 'regular'
    })}
    isActive={derived(page, ({ url: { pathname } }) => {
      if (pathname === '/app/starred') {
        return true
      }

      return false
    })}
  />

  <Navigation
    label="Shared"
    onclick={async () => goto('/app/shared')}
    icon={(isActive) => ({
      icon: 'user',
      thickness: isActive ? 'solid' : 'regular'
    })}
    isActive={derived(page, ({ url: { pathname } }) => {
      if (pathname === '/app/shared') {
        return true
      }

      return false
    })}
  />

  <Navigation
    label="Trash"
    onclick={async () => goto('/app/trash')}
    icon={(isActive) => ({
      icon: 'trash-can',
      thickness: isActive ? 'solid' : 'regular'
    })}
    isActive={derived(page, ({ url: { pathname } }) => {
      if (pathname === '/app/trash') {
        return true
      }

      return false
    })}
  />
{/if}

{#if $isMobile}
  <Navigation
    label="Notifications"
    onclick={async () => goto('/app/notification')}
    icon={(isActive) => ({
      icon: 'bell',
      thickness: isActive ? 'solid' : 'regular'
    })}
    isActive={derived(page, ({ url: { pathname } }) => {
      if (pathname === '/app/notification') {
        return true
      }

      return false
    })}
  />
{/if}

{#key $clientState[0]}
  {#await Promise.all([me(), amIAdmin()]) then [me, isAdmin]}
    {#if isAdmin}
      <Navigation
        label="Admin"
        onclick={async () => goto('/app/admin')}
        icon={(isActive) => ({
          icon: 'user-shield',
          thickness: 'solid'
        })}
        isActive={derived(page, ({ url: { pathname } }) => {
          if (pathname.startsWith('/app/admin')) {
            return true
          }

          return false
        })}
      />
    {/if}

    <Navigation
      label="Me"
      onclick={async () => goto(`/app/profile?id=${me.id}`)}
      icon={(isActive) => ({
        icon: 'user',
        thickness: isActive ? 'solid' : 'regular'
      })}
      isActive={derived(page, ({ url: { pathname } }) => {
        if (pathname === `/app/profile`) {
          return true
        }

        return false
      })}
    />
  {/await}
{/key}

<style lang="scss">
  div.navigation {
    flex-grow: 1;

    justify-content: safe center;

    overflow: hidden auto;
  }

  div.mobile {
    flex-direction: row;
  }
</style>
