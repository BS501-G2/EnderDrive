<script lang="ts">
  import type { Writable } from 'svelte/store'
  import { createNotificationContext, type NotificationContext } from './notification-context'
  import { onMount } from 'svelte'
  import { useClientContext } from '$lib/client/client'
  import { useDashboardContext, type DashboardContext } from '$lib/client/contexts/dashboard'
  import NotificationToast from './notification-toast.svelte'
  import { fly } from 'svelte/transition'
  import { useAppContext } from '$lib/client/contexts/app'
  import Overlay from '../overlay.svelte'

  const { isMobile } = useAppContext()

  const {
    notificationContext,
    dashboardContext
  }: { notificationContext: Writable<NotificationContext>; dashboardContext: DashboardContext } =
    $props()

  const { context, unread, toasts } = createNotificationContext()
  const { pushToast } = ($notificationContext = context)

  const {
    client: { setHandler },
    server
  } = useClientContext()

  async function load() {
    const notifications = await server.GetNotifications({
      ExcludeRead: true,
      ExcludeUnread: false
    })

    return notifications.length
  }

  onMount(() =>
    setHandler('Notify', async ({ NotificationId }) => {
      const notification = (
        await server.GetNotifications({
          NotificationId,
          ExcludeRead: false,
          ExcludeUnread: false,
          Pagination: {
            Count: 1
          }
        })
      )[0]

      pushToast(notification)
      unread.set(load())


      return {}
    })
  )

  onMount(() => unread.set(load()))
</script>

{#if $toasts.length}
  <Overlay notransition nooob y={$isMobile ? 0 : -1} x={$isMobile ? undefined : -1} nodim>
    <div class="toast-panel" class:mobile={$isMobile}>
      {#each $toasts as { notification, timeout, id } (id)}
        <NotificationToast
          {notification}
          notificationContext={$notificationContext}
          {dashboardContext}
          {timeout}
          dismiss={() => toasts.update((toasts) => toasts.filter((toast) => toast.id != id))}
        />
      {/each}
    </div>
  </Overlay>
{/if}

<style lang="scss">
  @use '../../global.scss' as *;

  div.toast-panel {
    gap: 8px;
    margin: 8px;

    box-sizing: border-box;
  }

  div.toast-panel.mobile {
    @include force-size(100dvw, &);
    margin: 8px 0 0 0;
  }
</style>
