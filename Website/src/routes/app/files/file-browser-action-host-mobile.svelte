<script lang="ts">
  import { useDashboardContext } from '$lib/client/contexts/dashboard'
  import { derived, writable, type Readable } from 'svelte/store'
  import AppButton from '../app-button.svelte'
  import { useFileBrowserContext, type FileBrowserAction } from '$lib/client/contexts/file-browser'
  import { onMount, type Snippet } from 'svelte'
  import Separator from '$lib/client/ui/separator.svelte'
  import Button from '$lib/client/ui/button.svelte'
  import Icon from '$lib/client/ui/icon.svelte'
  import { useAppContext } from '$lib/client/contexts/app'
  import Overlay from '../../overlay.svelte'
  import FileBrowserActionHostMobileExtra from './file-browser-action-host-mobile-extra.svelte'

  const {
    actions
  }: {
    actions: Readable<FileBrowserAction[]>
  } = $props()
  const { pushBottom } = useFileBrowserContext()
  const { isMobile } = useAppContext()

  const appButtons = derived(actions, (actions) =>
    actions.filter((action) => action.type === 'left-main' || action.type === 'right-main')
  )

  const buttons = derived(actions, (actions) => {
    const left = actions.filter((action) => action.type === 'left')
    const right = actions.filter((action) => action.type === 'right').slice(0)

    while (left.length > 3) {
      right.unshift(left.pop()!)
    }

    if (right.length === 1) {
      left.push(right.shift()!)
    }

    return [left, right]
  })

  onMount(() => pushBottom(button))

  let extraMenu = writable<{
    element: HTMLButtonElement
  } | null>(null)
</script>

{#each $appButtons as { id, icon, label, onclick } (id)}
  <AppButton {icon} {label} {onclick} />
{/each}

{#snippet button()}
  <div class="actions">
    {#each $buttons[0] as { id, snippet }, index (id)}
      {#if index != 0}
        <Separator vertical />
      {/if}

      <div class="button">
        {@render snippet()}
      </div>
    {/each}

    {#if $buttons[1].length}
      {#snippet foreground(view: Snippet)}
        <div class="extra" class:mobile={$isMobile}>
          {@render view()}
        </div>
      {/snippet}

      <div class="button">
        <Button
          onclick={({ currentTarget }) => {
            $extraMenu = {
              element: currentTarget
            }
          }}
          {foreground}
        >
          <Icon icon="ellipsis-vertical" thickness="solid" size="1.2em" />

          <p>More</p>
        </Button>
      </div>

      {#if $extraMenu != null}
        <FileBrowserActionHostMobileExtra
          element={$extraMenu.element}
          actions={$buttons[1]}
          ondismiss={() => {
            $extraMenu = null
          }}
        />
      {/if}
    {/if}
  </div>
{/snippet}

<style lang="scss">
  div.actions {
    flex-direction: row;
    flex-grow: 1;

    > div.button {
      flex-grow: 1;
    }
  }

  div.extra {
    padding: 8px;
    gap: 8px;

    flex-direction: row;
    align-items: center;
    justify-content: center;

    line-height: 1em;
  }

  div.extra.mobile {
    flex-direction: column;
  }
</style>
