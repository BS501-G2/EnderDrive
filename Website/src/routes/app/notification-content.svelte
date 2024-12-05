<script lang="ts">
  import UserLink from '$lib/client/model/user-link.svelte'
  import type { NotificationResource } from '$lib/client/resource'
  import Icon from '$lib/client/ui/icon.svelte'
  import { useClientContext } from '$lib/client/client'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'

  const {
    hidebell = false,
    shade = false,
    notification,
    viewer = false
  }: {
    hidebell?: boolean
    viewer?: boolean
    shade?: boolean
    notification: NotificationResource
  } = $props()

  const { server } = useClientContext()
</script>

<div class="notification">
  {#if true}
    <div class="icon" class:fixed-height={viewer}>
      <Icon icon="bell" thickness={shade || !notification.Read ? 'solid' : 'regular'}></Icon>
    </div>
  {/if}
  <div class="content">
    <b class="title"><UserLink noclickable userId={notification.ActorUserId} /></b>
    <p class="name" class:short={!viewer}>
      {notification.Data.Type} a file:
      {#await (async () => {
        const file = await server.GetFile({ FileId: notification.Data.FileId })

        return { file }
      })()}
        <LoadingSpinner size="1rem" />
      {:then { file }}
        <span class="file-link">{file.Name}</span>
      {/await}
    </p>
  </div>

  <div class="actions fixed-height"></div>
</div>

<style lang="scss">
  @use '../../global.scss' as *;

  div.fixed-height {
    @include force-size(&, 64px);
    box-sizing: border-box;
  }

  div.notification {
    flex-direction: row;

    min-width: 0;

    > div.icon {
      padding: 16px;

      align-self: center;
    }

    > div.content {
      flex-grow: 1;

      text-align: start;

      min-width: 0;

      padding: 4px;

      > b.title {
        font-weight: bolder;
      }

      > p.name {
        > span.file-link {
          font-weight: bolder;

          text-decoration: none;
          color: inherit;
        }
      }

      > p.name.short {
        text-overflow: ellipsis;
        text-wrap: nowrap;
        overflow: hidden;
      }
    }
  }
</style>
