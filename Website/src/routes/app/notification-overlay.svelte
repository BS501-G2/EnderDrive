<script lang="ts">
  import { type Snippet } from 'svelte'
  import Overlay from '../overlay.svelte'
  import NotificationHost from './notification-host.svelte'
  import Separator from '$lib/client/ui/separator.svelte'
  import { fly } from 'svelte/transition'
  import NotificationViewer from './notification-viewer.svelte'
  import type { NotificationContext } from './notification-context'

  const {
    focusId,
    ondismiss,
    notificationContext
  }: {
    focusId: string | null
    ondismiss: () => void
    notificationContext: NotificationContext
  } = $props()

  // const x = -(1 + (window.innerWidth - (boundElement.x + boundElement.width)));
  // const y = boundElement.y + boundElement.height;
</script>

<Overlay {ondismiss} x={-1} y={0} notransition>
  {#snippet children(windowButtons: Snippet)}
    <div
      class="notification"
      transition:fly|global={{
        x: 360
      }}
    >
      <div class="header">
        <h2>Notifications</h2>
        {@render windowButtons()}
      </div>

      <Separator horizontal />

      <div class="main">
        <NotificationViewer
          {ondismiss}
          subtractNumber={() => {
            notificationContext.unread.update((value) => value.then((value) => value - 1))
          }}
        />
      </div>
    </div>
  {/snippet}
</Overlay>

<style lang="scss">
  @use '../../global.scss' as *;

  div.notification {
    background-color: var(--color-9);
    color: var(--color-1);

    filter: drop-shadow(-2px 0 2px #0000007f);

    padding-top: env(titlebar-area-height, 0dvh);

    @include force-size(
      min(calc(100dvw - 64px), 360px),
      calc(100dvh - env(titlebar-area-height, 0dvh))
    );

    > div.header {
      flex-direction: row;
      align-items: center;

      > h2 {
        margin: 8px;
        font-weight: bolder;
        font-size: 1.5em;
        flex-grow: 1;
      }
    }

    > div.main {
      padding: 8px;

      flex-grow: 1;

      min-height: 0;
    }
  }
</style>
