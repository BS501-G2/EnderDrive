<script lang="ts">
  import { FileLogType, useServerContext, type FileLogResource } from '$lib/client/client'
  import UserLink from '$lib/client/model/user-link.svelte'
  import Icon from '$lib/client/ui/icon.svelte'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import moment from 'moment'
  const { fileLog }: { fileLog: FileLogResource } = $props()
  const server = useServerContext()
</script>

<div class="log-container">
  {#await (async () => {
    return { fileLog }
  })()}
    <div class="loading">
      <LoadingSpinner size="2rem" />
    </div>
  {:then { fileLog }}
    <div class="log">
      <Icon icon="pencil" thickness="solid" />
      <div class="message">
        <p class="message">
          <UserLink userId={fileLog.actorUserId} /> performed
          {[FileLogType[fileLog.type]]}
          operation
        </p>
        <p class="time">{moment(fileLog.createTime).fromNow()}</p>
      </div>
    </div>
  {/await}
</div>

<style lang="scss">
  div.log {
    flex-direction: row;

    padding: 8px;
    gap: 8px;

    > div.message{
      > p.time {
        font-size: 0.8em;

      }
    }
  }

  div.log:hover {
    background-color: var(--color-5);
  }
</style>
