<script lang="ts">
  import type { DashboardContext } from '$lib/client/contexts/dashboard'
  import UserLink from '$lib/client/model/user-link.svelte'
  import type { NotificationResource } from '$lib/client/resource'
  import AnimationFrame from '$lib/client/ui/animation-frame.svelte'
  import Button from '$lib/client/ui/button.svelte'
  import Icon from '$lib/client/ui/icon.svelte'
  import Window from '$lib/client/ui/window.svelte'
  import { onMount } from 'svelte'
  import { fly } from 'svelte/transition'
  import type { NotificationContext } from './notification-context'
  import { useAppContext } from '$lib/client/contexts/app'
  import NotificationContent from './notification-content.svelte'
  import { useClientContext } from '$lib/client/client'

  const {
    notification,
    notificationContext,
    dashboardContext,
    timeout,
    dismiss
  }: {
    notification: NotificationResource
    notificationContext: NotificationContext
    dashboardContext: DashboardContext
    timeout: number
    dismiss: () => void
  } = $props()

  const { notificationPage } = notificationContext
  const { isMobile } = useAppContext()
  const { server } = useClientContext()

  let jsTimeout: number | null = null

  onMount(() => {
    jsTimeout = setTimeout(dismiss, timeout)

    return () => {
      if (jsTimeout == null) {
        return
      }

      clearTimeout(jsTimeout)
    }
  })

  const endTime = Date.now() + timeout
  let timeLeft: number = $state(0)

  async function onclick(
    event: MouseEvent & {
      currentTarget: EventTarget & HTMLButtonElement
    }
  ) {
    await server.ReadNotification({ NotificationId: notification.Id })
    notificationContext.unread.update((value) => value.then((value) => value - 1))

    notificationPage.set({
      focusId: notification.Id
    })

    dismiss()
  }
</script>

<AnimationFrame
  onframe={() => {
    timeLeft = Math.max(endTime - Date.now(), 0)
  }}
/>

{#snippet highlight(shade: boolean)}
  <div class="toast" class:shade class:mobile={$isMobile}>
    <NotificationContent {notification} {shade} />
  </div>
{/snippet}

<Button {onclick}>
  <div class="toast-container" transition:fly|global={{ y: ($isMobile ? -1 : 1) * 48 }}>
    <div class="layer-container">
      <div class="layer">
        {@render highlight(false)}
      </div>
    </div>
    <div class="layer-container">
      <div
        class="layer"
        style:min-width="{Math.min(300, window.innerWidth - 18) * (timeLeft / timeout)}px"
        style:max-width="{Math.min(300, window.innerWidth - 18) * (timeLeft / timeout)}px"
      >
        {@render highlight(true)}
      </div>
    </div>
  </div>
</Button>

<style lang="scss">
  @use '../../global.scss' as *;

  div.toast-container {
    @include force-size(&, 48px);
    background-color: var(--color-9);
    color: var(--color-1);
  }

  div.toast {
    @include force-size(min(300px, calc(100dvw - 16px)), 48px);

    border: solid 1px var(--color-1);
    background-color: var(--color-9);
    color: var(--color-1);

    box-sizing: border-box;

    flex-direction: row;
    padding: 0px;
  }

  div.toast.mobile {
    @include force-size(calc(100dvw - 16px), &);
  }

  div.toast.shade {
    background-color: var(--color-1);
    color: var(--color-5);
  }

  div.layer {
    @include force-size(&, 50px);

    overflow: hidden;
  }

  div.layer-container {
    @include force-size(&, 0px);
  }
</style>
