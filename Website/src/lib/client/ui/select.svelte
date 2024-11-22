<script lang="ts" generics="T">
  import type { Snippet } from 'svelte'
  import { createSelectContext } from './select'
  import Button from './button.svelte'
  import { writable } from 'svelte/store'
  import Overlay from '../../../routes/overlay.svelte'
  import Icon from './icon.svelte'

  let {
    placeholder = 'Select an Option',
    set = $bindable(),
    selected = $bindable(),
    children
  }: {
    placeholder?: string
    selected: T | null
    set?: (value: T) => void
    children?: Snippet
  } = $props()

  const { options } = createSelectContext<T>()

  const show = writable(false)
  const selectedIndex = writable<number | null>(null)

  const buttonElement = writable<HTMLButtonElement>(null as never)

  const y = writable(0)
  const x = writable(0)
  const width = writable(0)

  $effect(() => {
    set = (value) => {
      $selectedIndex = $options.findIndex((e) => e.value == value)
    }
  })

  $effect(() => {
    selected = $selectedIndex == null ? null : $options[$selectedIndex]?.value
  })

  function updatePosition(buttonElement: HTMLButtonElement) {
    const box = buttonElement.getBoundingClientRect()

    x.set(box.x)
    y.set(box.y + box.height)
    width.set(box.width)
  }

  $effect(() => {
    if ($buttonElement != null) {
      updatePosition($buttonElement)
    }
  })
</script>

<svelte:window
  onresize={() => {
    if ($buttonElement != null) {
      updatePosition($buttonElement)
    }
  }}
/>

{#snippet foreground(view: Snippet)}
  <div class="container" class:this={$buttonElement}>
    <div class="selected">
      {@render view()}
    </div>
    <Icon icon="chevron-down" thickness="solid" size="0.8rem" />
  </div>
{/snippet}

<Button
  {foreground}
  onclick={() => {
    $show = !$show
  }}
  bind:buttonElement={$buttonElement}
>
  <div class="button">
    {$selectedIndex == null ? placeholder : ($options[$selectedIndex]?.label ?? 'Invalid Option')}
  </div>
</Button>

<div hidden>
  {@render children?.()}
</div>

{#if $show}
  <Overlay
    nodim
    ondismiss={() => {
      $show = false
    }}
    x={$x}
    y={$y}
  >
    <div class="menu" style:min-width="{$width}px" style:max-width="{$width}px">
      {#each $options as { id, label, snippet }, index}
        <Button
          onclick={() => {
            $selectedIndex = index
            $show = false
          }}
        >
          {@render snippet()}
        </Button>
      {/each}
    </div>
  </Overlay>
{/if}

<style lang="scss">
  div.button {
    flex-direction: row;
  }

  div.container {
    flex-grow: 1;
    flex-direction: row;
    align-items: center;

    padding: 8px;
    border: solid 1px var(--color-1);

    div.selected {
      flex-grow: 1;
    }
  }

  div.option {
    flex-grow: 1;
  }

  div.menu {
    background-color: var(--color-9);

    min-height: 0;
    max-height: 128px;

    overflow: hidden auto;
  }
</style>
