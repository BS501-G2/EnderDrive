<script lang="ts">
  import { useClientContext } from '$lib/client/client'
  import type { NotificationResource } from '$lib/client/resource'
  import Button from '$lib/client/ui/button.svelte'
  import Paginator from '$lib/client/ui/lazy-loader.svelte'
  import { type Snippet } from 'svelte'
  import NotificationContent from './notification-content.svelte'
  import { goto } from '$app/navigation'

  const { server } = useClientContext()
  const { ondismiss, subtractNumber }: { ondismiss: () => void; subtractNumber: () => void } =
    $props()

  let items: NotificationResource[] = $state([])
</script>

<div class="list">
  <Paginator
    load={async (offset) => {
      const notifications = await server.GetNotifications({
        ExcludeRead: false,
        ExcludeUnread: false,
        Pagination: {
          Count: 10,
          Offset: offset
        }
      })

      return notifications
    }}
    bind:items
    getKey={(item) => item.Id}
    vertical
    style="gap: 8px;"
  >
    {#snippet itemSnippet(item, index, key)}
      {#snippet foreground(view: Snippet)}
        <div class="foreground">
          {@render view()}
        </div>
      {/snippet}

      <Button
        {foreground}
        onclick={async () => {
          await server.ReadNotification({
            NotificationId: item.Id
          })

          if (!item.Read) {
            subtractNumber()
          }

          await goto(
            `/app/files?fileId=${item.Data.FileId}${item.Data.Type === 'Updated' ? `&dateId=${item.Data.FileDataId}` : ''}`
          )
          ondismiss()
        }}
      >
        <div class="indicator" class:unread={!item.Read}></div>

        <NotificationContent notification={item} hidebell viewer />
      </Button>
    {/snippet}
  </Paginator>
</div>

<style lang="scss">
  @use '../../global.scss' as *;

  div.list {
    flex-grow: 1;

    min-height: 0;
  }

  div.indicator {
    @include force-size(2px, &);
  }

  div.indicator.unread {
    background-color: var(--color-3);
  }

  div.foreground {
    flex-grow: 1;

    flex-direction: row;
  }
</style>
