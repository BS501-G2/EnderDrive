<script lang="ts" generics="T, K extends any = number">
  import { onMount, type Snippet } from 'svelte'
  import { useEvent } from '../utils'
  import { writable } from 'svelte/store'
  import LoadingSpinner from './loading-spinner.svelte'
  import Banner from './banner.svelte'

  const {
    items = $bindable(),
    class: className,
    style,
    load,

    itemSnippet,
    errorSnippet,
    loadingSnippet,
    doneSnippet,

    getKey,

    ...props
  }: {
    items: T[]
    class?: string
    style?: string
    load: (offset: number) => Promise<T[]> | T[]

    itemSnippet: Snippet<[item: T, index: number, key: K]>
    errorSnippet?: Snippet<[error: Error]>
    loadingSnippet?: Snippet<[]>
    doneSnippet?: Snippet

    getKey?: (item: T, index: number) => K
  } & ({ horizontal: boolean } | { vertical: boolean }) = $props()

  let list = $state<HTMLDivElement>(null as never)
  let isRunning = $state(false)
  let error = $state<Error | null>(null as never)
  let done: boolean = $state(false)

  function getScrollValues(): { scrollTop: number; scrollHeight: number; clientHeight: number } {
    const { scrollTop, scrollLeft, scrollHeight, scrollWidth, clientHeight, clientWidth } = list

    if ('horizontal' in props) {
      return {
        scrollHeight: scrollWidth,
        scrollTop: scrollLeft,
        clientHeight: clientWidth
      }
    } else if ('vertical' in props) {
      return {
        scrollHeight,
        scrollTop,
        clientHeight
      }
    } else {
      throw new Error('Must specify if horizontal or vertical')
    }
  }

  async function runPaginationLoop(list: HTMLDivElement) {
    if (isRunning || done) {
      return
    }

    error = null

    try {
      isRunning = true

      while (true) {
        const { scrollHeight, scrollTop, clientHeight } = getScrollValues()
        const a = scrollHeight - clientHeight * 1.25

        if (scrollTop <= a) {
          break
        }

        const newItems = await load(items.length)

        if (newItems.length === 0) {
          done = true
          break
        }

        items.push(...newItems)

        await new Promise<void>((resolve) => setTimeout(resolve, 1))
      }
    } finally {
      isRunning = false
    }
  }

  onMount(() => {
    runPaginationLoop(list)

    return useEvent(list, 'scroll', (list) => {
      return runPaginationLoop(list)
    })
  })
</script>

<svelte:window onresize={() => runPaginationLoop(list)} />

<div
  class="list{className && ` ${className}`}"
  style={style}
  bind:this={list}
  class:horizontal={'horizontal' in props}
  class:vertical={'vertical' in props}
>
  {#each items as item, index (getKey != null ? getKey(item, index) : index)}
    {@render itemSnippet(item, index, getKey != null ? getKey(item, index) : (index as K))}
  {/each}

  {#if isRunning}
    {@render (loadingSnippet ?? defaultLoadingSnippet)()}
  {:else if error != null}
    {@render (errorSnippet ?? defaultErrorSnippet)(error)}
  {:else if done}
    {@render doneSnippet?.()}
  {/if}
</div>

{#snippet defaultLoadingSnippet()}
  <div class="loading">
    <LoadingSpinner size="3rem" />
  </div>
{/snippet}

{#snippet defaultErrorSnippet()}
  <div class="error">
    <p>{''}</p>
  </div>
{/snippet}

<style lang="scss">
  div.list {
    flex-grow: 1;
  }

  div.loading {
    align-items: center;
    justify-content: center;
  }

  div.list.horizontal {
    flex-direction: row;
    min-width: 0;
    overflow: auto hidden;
  }

  div.list.vertical {
    min-height: 0;
    flex-direction: column;
    overflow: hidden auto;
  }
</style>
