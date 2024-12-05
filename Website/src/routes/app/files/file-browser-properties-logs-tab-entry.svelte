<script lang="ts">
  import { useClientContext } from '$lib/client/client';
  import UserLink from '$lib/client/model/user-link.svelte'
  import { FileLogType, type FileLogResource } from '$lib/client/resource'
  import Icon from '$lib/client/ui/icon.svelte'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import moment from 'moment'
  const { fileLog }: { fileLog: FileLogResource } = $props()
  const {server} = useClientContext()
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
          <UserLink userId={fileLog.ActorUserId} /> performed
          {[FileLogType[fileLog.Type]]}
          operation
        </p>
        <p class="time">{moment(fileLog.CreateTime).fromNow()}</p>
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
    color: var(--color-1);
  }
</style>
