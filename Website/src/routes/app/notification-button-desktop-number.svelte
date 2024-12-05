<script lang="ts">
  import Icon from '$lib/client/ui/icon.svelte'
  import type { NotificationContext } from './notification-context'

  const { context }: { context: NotificationContext } = $props()
  const { unread } = context

  let lastValue: number | null = $state(null)

  $effect(() => {
    $unread.then((value) => lastValue = value)
  })
</script>

{#snippet count(count: number)}
  {#if count}
    <Icon icon="circle" thickness="solid" size="4px" />
    <p>{count}</p>
  {/if}
{/snippet}

{#await $unread}
  {#if lastValue != null}
    {@render count(lastValue)}
  {/if}
{:then unread}
  {@render count((unread))}
{/await}
