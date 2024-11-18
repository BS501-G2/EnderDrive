<script lang="ts">
  import { onMount, type Snippet } from 'svelte'
  import { writable, type Writable } from 'svelte/store'
  import { tweened } from 'svelte/motion'
  import LandingPages from './landing-pages.svelte'
  import { useAppContext } from '$lib/client/contexts/app'
  import {
    createLandingContext,
    type LoginContext
  } from '$lib/client/contexts/landing'
  import SiteLogo from './site-logo.svelte'
  import { useColorContext } from '$lib/client/contexts/colors'
  import LandingPagesExpando from './landing-pages-expando.svelte'
  import LandingActions from './landing-actions.svelte'
  import LoginDialog from './login-dialog.svelte'
  import { page } from '$app/stores'
  import Overlay from '../overlay.svelte'

  const {
    pages,
    buttons,
    footer,
    login,
    context: { openLogin, closeLogin }
  } = createLandingContext()
  const { isDesktop, isMobile, pushTitle } = useAppContext()
  const { useColor } = useColorContext()

  const currentPage = writable(0)

  const opacity = tweened(0)

  const {
    children
  }: {
    children: Snippet
  } = $props()

  onMount(() => {
    const {
      url: { searchParams }
    } = $page

    if (searchParams.has('login')) {
      openLogin()
    }
  })

  onMount(() => pushTitle('Welcome!'))
</script>

<svelte:window
  onscroll={({ currentTarget: { scrollY } }) => {
    $opacity = scrollY > 64 ? 1 : 0
  }}
/>

<div
  class="topbar-container"
  style:backdrop-filter="blur({(1 - $opacity) * 4}px)"
  style:background-color="rgba({$useColor(0, $opacity).join(',')})"
>
  <div
    class="topbar"
    style:filter="drop-shadow({(1 - $opacity) * 2}px
    {(1 - $opacity) * 2}px
    {(1 - $opacity) * 2}px black)"
  >
    <div class="left bar">
      <SiteLogo />

      {#if $isDesktop}
        <LandingPages {pages} {currentPage} />
      {/if}
    </div>
    <div class="right bar">
      {#if $isMobile}
        <LandingPagesExpando {currentPage} {pages} />
      {/if}

      <LandingActions {buttons} {opacity} />
    </div>
  </div>
</div>

<div class="content-container">
  {#each $pages as { id, content } (id)}
    {@render content()}
  {/each}
</div>

<div class="footer">
  {#each $footer as { id, name, content }}
    <div class="footer-entry">
      <div class="title">
        {name}
      </div>

      <div class="content">
        {@render content()}
      </div>
    </div>
  {/each}
</div>

{#if $login != null}
  <Overlay
    ondismiss={() => {
      closeLogin()
    }}
  >
    {#snippet children(windowButtons: Snippet)}
      <LoginDialog login={login as Writable<LoginContext>} {windowButtons} />
    {/snippet}
  </Overlay>
{/if}

{@render children()}

<style lang="scss">
  @use '../../global.scss' as *;

  div.topbar-container {
    flex-direction: column;
    align-items: center;

    position: fixed;
    left: 0;
    top: 0;

    padding: 0px 16px;
    padding-top: env(titlebar-area-height);
    box-sizing: border-box;

    color: var(--color-5);
    background-color: var(--color-3);

    -webkit-app-region: drag;

    @include force-size(100%, &);

    z-index: 1;

    > div.topbar {
      flex-direction: row;

      @include force-size(min(100%, 1280px, 100%), 64px);

      > div.bar {
        flex-direction: row;
        flex-grow: 1;

        gap: 16px;
      }

      > div.bar.right {
        flex-direction: row-reverse;

        gap: 8px;
      }
    }
  }

  div.content-container {
    flex-direction: column;
    box-sizing: border-box;

    gap: 64px;

    @include force-size(&, 100dvh);
  }

  div.footer {
    min-height: 128px;
    > div.footer-entry {
      > div.header {
      }

      > div.content {
      }
    }
  }
</style>
