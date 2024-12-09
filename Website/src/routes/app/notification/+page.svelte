<script lang="ts">
  import { goto } from '$app/navigation'
  import { useAppContext } from '$lib/client/contexts/app'
  import NotificationViewer from '../notification-viewer.svelte'

  const { pushTitle, isMobile } = useAppContext()

  $effect(() => pushTitle('Notifications'))

  $effect(() =>
    isMobile.subscribe((isMobile) => {
      if (!isMobile) {
        goto('/app')
      }
    })
  )
</script>

<NotificationViewer ondismiss={() => {}} />
