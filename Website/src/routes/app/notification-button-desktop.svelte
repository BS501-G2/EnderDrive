<script lang="ts">
  import { useAppContext } from '$lib/client/contexts/app'
  import { useDashboardContext } from '$lib/client/contexts/dashboard'
  import Button from '$lib/client/ui/button.svelte'
  import Icon from '$lib/client/ui/icon.svelte'
  import { onMount, type Snippet } from 'svelte'
  import { get, writable, type Writable } from 'svelte/store'
  import NotificationOverlay from './notification-overlay.svelte'
  import NotificationButtonDesktopNumber from './notification-button-desktop-number.svelte'
  import type { NotificationContext } from './notification-context'

  const { pushDesktopTopRight, notification } = useDashboardContext()

  onMount(() => pushDesktopTopRight(desktop))

  const notificationContext = $derived($notification)
  const element = $derived($notification.desktopButtonElement)
  const notificationPage = $derived(notificationContext.notificationPage)
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
        notificationPage.set({
          focusId: null
        })}
    >
      <Icon icon="bell" />

      {#if $notification != null}
        <NotificationButtonDesktopNumber context={$notification} />
      {/if}
    </Button>
  </div>

  {#if $notificationPage}
    <NotificationOverlay
      focusId={$notificationPage?.focusId ?? null}
      notificationContext={$notification}
      ondismiss={() => {
        $notificationPage = null
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
    flex-direction: row;
    align-items: center;

    padding: 8px;
    gap: 8px;
  }
</style>
