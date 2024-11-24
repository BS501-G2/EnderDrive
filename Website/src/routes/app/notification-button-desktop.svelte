<script lang="ts">
  import { useAppContext } from '$lib/client/contexts/app'
  import { useDashboardContext } from '$lib/client/contexts/dashboard'
  import Button from '$lib/client/ui/button.svelte'
  import Icon from '$lib/client/ui/icon.svelte'
  import { onMount, type Snippet } from 'svelte'
  import { derived, writable, type Writable } from 'svelte/store'
  import NotificationOverlay from './notification-overlay.svelte'

  const { pushDesktopTopRight } = useDashboardContext()

  onMount(() => pushDesktopTopRight(desktop))

  const notification: Writable<{
    element: HTMLElement
  } | null> = writable(null)
</script>

{#snippet desktop()}
  {#snippet buttonForeground(view: Snippet)}
    <div class="button-foreground">
      {@render view()}
    </div>
  {/snippet}

  <div class="notification">
    <Button
      foreground={buttonForeground}
      onclick={async (event) =>
        notification.set({
          element: event.currentTarget
        })}
    >
      <Icon icon="bell" />
    </Button>
  </div>

  {#if $notification}
    <NotificationOverlay
      element={$notification.element}
      ondismiss={() => {
        $notification = null
      }}
    />
  {/if}
  
{/snippet}

<style lang="scss">
  div.notification {
    -webkit-app-region: no-drag;

    flex-direction: row;

    align-items: center;
  }

  div.button-foreground {
    padding: 8px;
  }
</style>
