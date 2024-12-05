<script lang="ts" generics="T extends any">
  import { onMount, type Snippet } from 'svelte'
  import { useAppContext } from '$lib/client/contexts/app'
  import { scale } from 'svelte/transition'
  import { createOverlayContext } from '../lib/client/contexts/overlay'
  import Button from '../lib/client/ui/button.svelte'
  import Icon from '../lib/client/ui/icon.svelte'

  const { pushOverlayContent } = useAppContext()
  const {
    children,
    ondismiss,
    nodim = false,
    notransition = false,
    noshadow = false,
    x,
    y,
    'disable-x': disableX = false,
    nooob = false,
    payload
  }: {
    children?: Snippet<[windowButtons: Snippet, payload: T]>
    ondismiss?: () => void
    nodim?: boolean
    noshadow?: boolean
    notransition?: boolean
    x?: number
    y?: number
    'disable-x'?: boolean,
    payload?: T
    nooob?: boolean
  } = $props()

  let {
    horizontalAlign,
    paddingLeft,
    paddingRight
  }: {
    horizontalAlign: 'flex-start' | 'center' | 'flex-end'
    paddingLeft: number
    paddingRight: number
  } = $derived(
    x == null
      ? {
          horizontalAlign: 'center',
          paddingLeft: 0,
          paddingRight: 0
        }
      : x < 0
        ? {
            horizontalAlign: 'flex-end',
            paddingLeft: 0,
            paddingRight: -(x + 1)
          }
        : {
            horizontalAlign: 'flex-start',
            paddingLeft: x,
            paddingRight: 0
          }
  )

  let {
    verticalAlign,
    paddingTop,
    paddingBottom
  }: {
    verticalAlign: 'flex-start' | 'center' | 'flex-end'
    paddingTop: number
    paddingBottom: number
  } = $derived(
    y == null
      ? {
          verticalAlign: 'center',
          paddingTop: 0,
          paddingBottom: 0
        }
      : y < 0
        ? {
            verticalAlign: 'flex-end',
            paddingTop: 0,
            paddingBottom: -(y + 1)
          }
        : {
            verticalAlign: 'flex-start',
            paddingTop: y,
            paddingBottom: 0
          }
  )

  onMount(() => pushOverlayContent(overlay, !nodim, !noshadow))


  const {
    buttons,
    context: { pushButton }
  } = createOverlayContext()

  $effect(() => {
    const ondestroy: (() => void)[] = []

    if (!disableX) {
      ondestroy.push(
        pushButton(
          'test',
          {
            icon: 'xmark',
            thickness: 'solid'
          },
          () => ondismiss?.()
        )
      )
    }

    return () => ondestroy.forEach((destroy) => destroy())
  })
</script>

{#snippet windowButtons()}
  <div class="window-buttons">
    {#each $buttons as { id, tooltip, icon, onclick }, index (id)}
      {#snippet background(view: Snippet)}
        <div class="background">
          {@render view()}
        </div>
      {/snippet}

      {#snippet foreground(view: Snippet)}
        <div class="window-button">
          {@render view()}
        </div>
      {/snippet}

      <Button {onclick} {background} {foreground}>
        <Icon {...icon} size="1em" />
      </Button>
    {/each}
  </div>
{/snippet}

{#snippet overlay()}
  <div class="overlay-bounds">
    <button
      class="overlay-container"
      class:nooob
      style:align-items="safe {horizontalAlign}"
      style:justify-content="safe {verticalAlign}"
      style:padding-top="{paddingTop}px"
      style:padding-bottom="{paddingBottom}px"
      style:padding-left="{paddingLeft}px"
      style:padding-right="{paddingRight}px"
      class:dim={!nodim}
      onclick={({ currentTarget, target }) => {
        if (currentTarget != target) {
          return
        }

        ondismiss?.()
      }}
    >
      {#if !notransition}
        <div
          class="overlay"
          transition:scale|global={{
            duration: 250,
            start: 0.95
          }}
        >
          {@render children?.(windowButtons, payload as never)}
        </div>
      {:else}
        <div class="overlay">
          {@render children?.(windowButtons, payload as never)}
        </div>
      {/if}
    </button>
  </div>
{/snippet}

<style lang="scss">
  @use '../global.scss' as *;

  div.window-buttons {
    flex-direction: row-reverse;

    div.background {
      flex-grow: 1;
    }

    div.window-button {
      padding: 8px 16px;

      align-items: center;
      justify-content: center;
    }
  }

  div.overlay-bounds {
    min-height: 0;
    max-height: 0;
  }

  button.overlay-container {
    display: flex;
    flex-direction: column;

    pointer-events: visible;

    text-align: start;

    // overflow: auto;

    border: none;
    outline: none;

    @include force-size(100dvw, 100dvh);
  }

  button.overlay-container.nooob {
    pointer-events: none;

    div.overlay {
      pointer-events: auto;
    }
  }

  button.overlay-container.dim {
    background-color: #00000025;
  }

  // button.overlay-container.shadow {
  //   filter: drop-shadow(2px 2px 4px #0000007f);
  // }

  div.overlay {
    display: flex;
    flex-direction: column;

    min-width: 0px;
    min-height: 0px;
    // max-width: 100dvw;
    // max-height: 100dvh;
  }
</style>
