<script lang="ts">
  import type { Snippet } from 'svelte'
  import LoadingSpinner from './loading-spinner.svelte'
  import { writable } from 'svelte/store'
  import Window from './window.svelte'
  import Separator from './separator.svelte'
  import { dev } from '$app/environment'

  const clickButton = () => {
    $button.click()
  }

  let {
    hint,
    children,
    background,
    foreground,
    onclick,
    click = $bindable(),
    buttonElement = $bindable(),
    disabled = false,
    onerror,
    reset = $bindable()
  }: {
    hint?: string
    children?: Snippet
    background?: Snippet<[content: Snippet, error: boolean]>
    foreground?: Snippet<[content: Snippet, error: boolean]>
    onclick: (
      event: MouseEvent & {
        currentTarget: EventTarget & HTMLButtonElement
      }
    ) => void | Promise<void>
    onerror?: (error?: Error) => {}
    click?: () => void
    buttonElement?: HTMLButtonElement
    disabled?: boolean
    reset?: () => void
  } = $props()

  const promise = writable<Promise<void> | null>(null)
  const error = writable<Error | null>(null)

  $effect(() => {
    reset = () => {
      $error = null
    }
  })

  $effect(() => {
    click = clickButton
  })

  $effect(() => {
    buttonElement = $button
  })

  $effect(() => {
    if ($error != null) {
      console.log($error)
    }
  })

  const button = writable<HTMLButtonElement>(null as never)
</script>

<button
  bind:this={$button}
  class:disabled
  title={hint}
  {disabled}
  onclick={(event) => {
    try {
      if ($promise != null) {
        return
      }

      $error = null
      const resultPromise = onclick(event)

      if (resultPromise instanceof Promise) {
        $promise = resultPromise

        void (async () => {
          try {
            await $promise
          } catch (e: any) {
            $error = e
          } finally {
            $promise = null
          }
        })()
      }
    } catch (e: any) {
      $error = e
      onerror?.($error!)
    }
  }}
>
  {#snippet backgroundContent()}
    <div class="background" class:error={$error != null} class:busy={$promise != null}>
      {#snippet foregroundContent()}
        {#if $error != null}
          {$error.message}
        {:else if $promise != null}
          <LoadingSpinner size="1em" />
        {:else}
          {@render children?.()}
        {/if}
      {/snippet}

      {#if foreground != null}
        {@render foreground(foregroundContent, $error != null)}
      {:else}
        {@render foregroundContent()}
      {/if}
    </div>
  {/snippet}

  {#if background != null}
    {@render background(backgroundContent, error != null)}
  {:else}
    {@render backgroundContent()}
  {/if}
</button>

{#if $error != null && dev}
  <Window
    ondismiss={() => {
      $error = null
    }}
    title="An Error Occured"
  >
    <div class="error-container">
      <p>An error occured on the click hook.</p>
      <div class="error">
        <pre class="error-message">{$error?.message}</pre>

        <Separator horizontal />

        <pre class="error-stack">{$error?.stack}</pre>
      </div>
    </div>
  </Window>
{/if}

<style lang="scss">
  div.error-container {
    max-width: 640px;
    max-height: 320px;
    gap: 8px;

    > div.error {
      padding: 8px;
      gap: 8px;

      overflow: hidden auto;

      background-color: var(--color-6);
      color: var(--color-5);

      > pre.error-message {
        font-size: 1.2em;
        font-weight: bolder;
        white-space: pre-wrap;
      }

      > pre.error-stack {

        white-space: pre-wrap;
      }
    }
  }

  button {
    -webkit-app-region: no-drag;

    display: flex;
    flex-direction: column;
    align-items: stretch;

    border: none;
    cursor: pointer;

    flex-shrink: 0;
    // border-radius: 8px;
    padding: 0;

    overflow: hidden;

    transition-property: scale;

    div.background.busy {
      cursor: not-allowed;
    }

    div.background {
      transition-property: background-color, color;
      flex-direction: row;

      justify-content: center;

      flex-grow: 1;
      height: 100%;
    }

    div.background.error {
      background-color: var(--color-6);
    }

    div.background.busy {
      cursor: not-allowed;
    }
  }

  div.background,
  button {
    transition: linear;
    transition-duration: 150ms;
  }

  button:hover {
    div.background {
      background-color: rgba(0, 0, 0, 0.25);
    }
  }

  button:focus {
    div.background {
      outline: #ffffff;
    }
  }

  button:active {
    scale: 0.95;

    div.background {
      background-color: rgba(0, 0, 0, 0.75);
      color: var(--color-5);
    }
  }

  button.disabled {
    cursor: not-allowed;

    div.background {
      background-color: rgba(0, 0, 0, 0.25);
    }
  }
</style>
