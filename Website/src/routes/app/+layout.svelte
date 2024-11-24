<script lang="ts">
  import { goto } from '$app/navigation'
  import { page } from '$app/stores'

  import { useClientContext, useServerContext } from '$lib/client/client'
  import { useAppContext, ViewMode, WindowMode } from '$lib/client/contexts/app'
  import { createDashboardContext } from '$lib/client/contexts/dashboard'
  import Button from '$lib/client/ui/button.svelte'
  import Favicon from '$lib/client/ui/favicon.svelte'
  import Icon from '$lib/client/ui/icon.svelte'
  import Search from './search.svelte'
  import { onMount, type Snippet } from 'svelte'
  import { createNavigationContext } from '$lib/client/contexts/navigation'
  import NavigationHost from './navigation-host.svelte'
  import AppButtonHost from './app-button-host.svelte'
  import NotificationButtonDesktop from './notification-button-desktop.svelte'
  import ProgressHost from './progress-host.svelte'
  import User from './user.svelte'
  import LogoutConfirmation from './logout-confirmation.svelte'
  import { writable } from 'svelte/store'
  import InstallButtonDesktop from './install-button-desktop.svelte'
  import AppButton from './app-button.svelte'
  import { createSyncContext } from './sync'
  import News from './news.svelte'
  const {
    mobileAppButtons,
    mobileTopLeft,
    mobileTopRight,
    mobileBottom,
    desktopSide,
    desktopTopLeft,
    desktopTopMiddle,
    desktopTopRight,
    backgroundTasks
  } = createDashboardContext()
  const { navigationEntries } = createNavigationContext()
  const {
    isMobile,
    isDesktop,
    isCustomBar,
    pwaAvailable,
    viewMode,
    windowMode,
    isFullscreen,
    titleStack
  } = useAppContext()
  const { authentication } = useClientContext()
  const { me, didIAgree } = useServerContext()

  const {
    children
  }: {
    children: Snippet
  } = $props()

  if ('showOpenFilePicker' in window) {
    createSyncContext(useServerContext())
  }

  const logoutConfirmation = writable(false)

  onMount(() =>
    authentication.subscribe(async (authentication) => {
      if (authentication == null) {
        await goto(`/landing?login&return=${encodeURIComponent($page.url.pathname)}`, {
          replaceState: true
        })
      } else if (!(await didIAgree()) && !$page.url.pathname.startsWith('/agreement')) {
        await goto(`/app/agreement?return=${encodeURIComponent($page.url.pathname)}`)
      }
    })
  )

  onMount(() => {
    window.document.body.classList.add('app')

    return () => {
      window.document.body.classList.remove('app')
    }
  })
</script>

{#if $authentication != null}
  <div class="dashboard">
    <div class="top">
      <div class="nav">
        {#snippet navButton(view: Snippet)}
          <div class="button">
            {@render view()}
          </div>
        {/snippet}
        <Button foreground={navButton} onclick={() => window.history.back()}>
          <Icon icon="chevron-left" thickness="solid" />
        </Button>

        {#if !$isMobile}
          <Button foreground={navButton} onclick={() => window.history.forward()}>
            <Icon icon="chevron-right" thickness="solid" />
          </Button>
        {/if}
      </div>

      {#if $isFullscreen || $isCustomBar || $isMobile}
        <div class="logo">
          {#if ($isFullscreen || $isCustomBar) && !$isMobile}
            <Favicon size={16} />
          {/if}

          <p class="title">
            {#if $isMobile}
              {$titleStack
                .map((e) => e.title)
                .slice($titleStack.length > 1 ? 1 : 0)
                .toReversed()
                .join(' - ')}
            {:else}
              EnderDrive
            {/if}
          </p>
        </div>
      {/if}

      <div class="left">
        {#if $isDesktop}
          {#each $desktopTopLeft as { id, snippet } (id)}
            {@render snippet()}
          {/each}
        {:else if $isMobile}
          {#each $mobileTopLeft as { id, snippet } (id)}
            {@render snippet()}
          {/each}
        {/if}
      </div>

      <div class="center">
        {#if $isDesktop}
          {#each $desktopTopMiddle as { id, snippet } (id)}
            {@render snippet()}
          {/each}
        {/if}
      </div>

      <div class="right">
        {#if $isDesktop}
          {#each $desktopTopRight as { id, snippet } (id)}
            {@render snippet()}
          {/each}
        {:else if $isMobile}
          {#each $mobileTopRight as { id, snippet } (id)}
            {@render snippet()}
          {/each}
        {/if}
      </div>
    </div>

    <div class="separator"></div>

    <div class="middle">
      {#if !$isMobile}
        <div class="side">
          <NavigationHost {navigationEntries} />

          {#each $desktopSide as { id, snippet } (id)}
            {@render snippet()}
          {/each}
        </div>

        <div class="separator"></div>
      {/if}

      <div class="main">
        {@render children()}
      </div>
    </div>

    {#if $isMobile}
      <div class="separator"></div>
      <div class="bottom">
        <NavigationHost {navigationEntries} />

        {#each $mobileBottom as { id, snippet } (id)}
          {@render snippet()}
        {/each}
      </div>
    {/if}
  </div>

  <Search />
  <AppButtonHost {mobileAppButtons} />
  <ProgressHost tasks={backgroundTasks} />
  <News />

  {#if $isDesktop}
    <NotificationButtonDesktop />

    {#if $pwaAvailable != null}
      <InstallButtonDesktop install={$pwaAvailable.install} />
    {/if}

    {#await me() then user}
      {#if user != null}
        <User bind:logoutConfirmation={$logoutConfirmation} {user} />
      {/if}
    {/await}
  {:else if $isMobile}
    {#if $pwaAvailable}
      <AppButton
        label="Install"
        icon={{ icon: 'download', thickness: 'solid' }}
        onclick={$pwaAvailable.install}
      />
    {/if}
  {/if}

  {#if $logoutConfirmation}
    <LogoutConfirmation
      ondismiss={() => {
        $logoutConfirmation = false
      }}
    />
  {/if}
{/if}

<style lang="scss">
  @use '../../global.scss' as *;

  :global(body.app) {
    min-height: 0;
    min-width: 0;
  }

  div.dashboard {
    flex-grow: 1;

    overflow: hidden;
    @include force-size(100dvw, 100dvh);

    > div.separator {
      background-color: var(--color-5);

      @include force-size(&, 1px);
    }

    > div.top {
      flex-direction: row;
      -webkit-app-region: drag;

      @include force-size(env(titlebar-area-width), &);
      min-height: 48px;
      gap: 8px;
      padding: 0 8px;
      box-sizing: border-box;

      > div.nav {
        margin: 8px 0;
        gap: 8px;
        -webkit-app-region: no-drag;
        line-height: 1em;

        flex-direction: row;

        div.button {
          flex-grow: 1;
          flex-direction: row;
          align-items: center;

          padding: 8px;
        }
      }

      > div.logo {
        flex-direction: row;

        align-items: center;

        gap: 8px;
        padding: 8px;

        min-width: 0;

        > p.title {
          text-overflow: ellipsis;
          text-wrap: nowrap;
          overflow: hidden;

          flex-shrink: 1;

          min-width: 0;
        }
      }

      > div.left,
      > div.right,
      > div.center {
        gap: 8px;
      }

      > div.left {
        flex-direction: row;
      }

      > div.center {
        flex-direction: row;
        align-items: center;
        justify-content: center;
        flex-grow: 1;
      }

      > div.right {
        flex-direction: row;
        justify-content: end;
      }
    }

    > div.middle {
      flex-grow: 1;
      flex-direction: row;

      min-height: 0;

      > div.side {
        -webkit-app-region: drag;

        @include force-size(72px, &);
      }

      > div.separator {
        background-color: var(--color-5);

        @include force-size(1px, &);
      }

      > div.main {
        flex-grow: 1;

        overflow: hidden auto;

        min-width: 0;
        min-height: 0;
      }
    }

    > div.bottom {
      min-height: 64px;
    }
  }
</style>
