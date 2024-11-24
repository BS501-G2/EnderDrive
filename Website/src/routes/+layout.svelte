<script lang="ts">
  import '@fortawesome/fontawesome-free/js/all'
  import 'reset-css/reset.css'

  import { onMount, type Snippet } from 'svelte'
  import { get, writable } from 'svelte/store'
  import { createColorContext } from '../lib/client/contexts/colors'
  import { createAppContext, ViewMode, WindowMode } from '$lib/client/contexts/app'
  import { createClientContext } from '$lib/client/client'
  import OverlayHost from './overlay-host.svelte'
  import Pwa from './app/pwa.svelte'
  import { navigating } from '$app/stores'
  import Overlay from './overlay.svelte'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'

  const {
    viewMode,
    windowMode,
    overlay,
    titleStack,
    pwaAvailable,
    context: { pushTitle }
  } = createAppContext()

  function triggerUpdateViewMode(window: Window) {
    let newMode: ViewMode = get(viewMode)

    if (window.innerWidth >= 1280) {
      newMode = ViewMode.Desktop
    } else if (window.innerWidth >= 768) {
      newMode = ViewMode.LimitedDesktop
    } else if (window.innerWidth < 768) {
      newMode = ViewMode.Mobile
    } else {
      newMode = ViewMode.None
    }

    if (newMode !== get(viewMode)) {
      viewMode.set(newMode)
    }
  }

  function triggerUpdateWindowMode(window: Window) {
    let newMode: WindowMode = WindowMode.Normal

    if (window.matchMedia('(display-mode: window-controls-overlay)').matches) {
      newMode |= WindowMode.CustomBar
    }

    if (window.matchMedia('(display-mode: fullscreen)').matches) {
      newMode |= WindowMode.Fullscreen
    }

    if (window.matchMedia('(display-mode: standalone)').matches) {
      newMode |= WindowMode.Minimal
    }

    if (newMode !== get(windowMode)) {
      windowMode.set(newMode)
    }
  }

  const { printStyleHTML, useCssColor } = createColorContext()
  // const {} = createSyncContext()
  const {} = createClientContext()
  const {
    children
  }: {
    children: Snippet
  } = $props()

  const showNav = writable(false)

  onMount(() => {
    triggerUpdateViewMode(window)
    triggerUpdateWindowMode(window)
  })

  onMount(() => pushTitle('EnderDrive'))

  onMount(() => {
    let timeout: number | null = null

    return navigating.subscribe((isNav) => {
      if (isNav) {
        if (timeout == null) {
          timeout = setTimeout(() => {
            $showNav = true
          }, 500)
        }
      } else {
        if (timeout != null) {
          clearTimeout(timeout)
          timeout = null
        }

        $showNav = false
      }
    })
  })
</script>

<svelte:head>
  <link rel="icon" href="/favicon.svg" type="image/xml+svg" />
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <link
    rel="manifest"
    href="/manifest?theme-color={$useCssColor(8, 0)}&background-color={$useCssColor(8, 0)}"
  />
  <title>
    {$titleStack
      .toReversed()
      .map((e) => e.title)
      .join(' - ')}
  </title>
  {@html $printStyleHTML()}
</svelte:head>

<svelte:window
  onresize={() => {
    triggerUpdateViewMode(window)
    triggerUpdateWindowMode(window)
  }}
/>
{@render children()}

<OverlayHost {overlay} />
<Pwa {pwaAvailable} />

{#if $showNav}
  <Overlay>
    <div class="nav-indicator">
      <LoadingSpinner size="3rem" />
    </div>
  </Overlay>
{/if}

<style lang="scss">
  @use '../global.scss' as *;

  div.nav-indicator {
    padding: 16px;

    background-color: var(--color-9);
    color: var(--color-1);

    border-radius: 50%;
  }

  :global(div) {
    display: flex;
    flex-direction: column;
  }

  :global(input, button) {
    background-color: inherit;
    color: inherit;
  }

  :global(body) {
    display: flex;
    flex-direction: column;
    box-sizing: border-box;

    background-color: var(--color-9);
    color: var(--color-1);

    min-width: 320px;
    min-height: 100dvh;

    user-select: none;
  }
</style>
