<script lang="ts">
  import { goto } from '$app/navigation'
  import { page } from '$app/stores'

  import { useClientContext } from '$lib/client/client'
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
  import News from './news.svelte'
  import NotificationHost from './notification-host.svelte'
  import { type NotificationContext } from './notification-context'
  import Agreement from './agreement.svelte'
  import StatsHost from './stats-host.svelte'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import Separator from '$lib/client/ui/separator.svelte'

  const notificationContext = writable<NotificationContext>(null as never)

  const dashboard = createDashboardContext(notificationContext)
  const {
    mobileAppButtons,
    mobileTopLeft,
    mobileTopRight,
    mobileBottom,
    desktopSide,
    desktopTopLeft,
    desktopTopMiddle,
    desktopTopRight,
    desktopBottom,
    backgroundTasks
  } = dashboard
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
  const { authentication, server, clientState } = useClientContext()

  const {
    children
  }: {
    children: Snippet
  } = $props()

  const logoutConfirmation = writable(false)
  const didIAgree = writable(Promise.resolve(false))

  onMount(() =>
    authentication.subscribe(async (authentication) => {
      if (authentication == null) {
        await goto(`/landing?login&return=${encodeURIComponent($page.url.pathname)}`, {
          replaceState: true
        })
      }
      didIAgree.set(server.DidIAgree({}))
    })
  )

  onMount(() => {
    window.document.body.classList.add('app')

    return () => {
      window.document.body.classList.remove('app')
    }
  })
</script>

{#await $didIAgree then result}
  {#if !result}
    <Agreement refresh={() => didIAgree.set(server.DidIAgree({}))} />
  {/if}
{/await}

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

    {#if $clientState[0] !== 'connnected'}
      <Separator horizontal />

      <div class="disconnection-message">
        {#if $clientState[0] === 'connecting'}
          <LoadingSpinner size="1rem" />
        {/if}

        <p>
          Currently {$clientState[0]}.
        </p>

        {#if $clientState[0] === 'disconnected'}
          <Button onclick={$clientState[2]}>Reconnect</Button>
        {/if}
      </div>
    {/if}

    <Separator horizontal />

    <div class="middle">
      {#if !$isMobile}
        <div class="side">
          <NavigationHost {navigationEntries} />

          {#each $desktopSide as { id, snippet } (id)}
            {@render snippet()}
          {/each}
        </div>

        <Separator vertical />
      {/if}

      <div class="main">
        {@render children()}
      </div>
    </div>
    {#if $isDesktop && $desktopBottom.length}
      <Separator horizontal />
      <div class="desktop-bottom">
        <div class="left section">
          {#each $desktopBottom.filter((e) => e.arrangement === 'left') as { id, snippet } (id)}
            {@render snippet()}
          {/each}
        </div>
        <div class="right section">
          {#each $desktopBottom.filter((e) => e.arrangement === 'right') as { id, snippet } (id)}
            {@render snippet()}
          {/each}
        </div>
      </div>
    {/if}

    {#if $isMobile}
      <Separator horizontal />
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
  <StatsHost />
  <News />
  <NotificationHost {notificationContext} dashboardContext={dashboard.context} />

  {#if $isDesktop}
    <NotificationButtonDesktop />

    {#if $pwaAvailable != null}
      <InstallButtonDesktop install={$pwaAvailable.install} />
    {/if}

    {#await server.Me({}) then user}
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

    > div.disconnection-message {
      flex-direction: row;
      justify-content: safe center;

      padding: 8px;
      gap: 8px;

      background-color: var(--color-6);
      color: var(--color-5);
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

    > div.desktop-bottom {
      @include force-size(&, 32px);

      gap: 8px;
      padding: 0 8px;

      flex-direction: row;

      > div.section {
        flex-direction: row;
        flex-grow: 1;
      }

      div.right.section {
        justify-content: flex-end;
      }
    }
  }
</style>
