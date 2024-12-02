<script lang="ts">
  import type { Writable } from 'svelte/store'
  import { createNotificationContext, type NotificationContext } from './notification-context'
  import { onMount } from 'svelte'
  import { useClientContext } from '$lib/client/client'

  const { context }: { context: Writable<NotificationContext> } = $props()

  const {} = ($context = createNotificationContext())
  const {
    client: { setHandler }
  } = useClientContext()

  onMount(() =>
    setHandler('Notify', async (data) => {
      return {}
    })
  )
</script>
