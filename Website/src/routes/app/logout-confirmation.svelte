<script lang="ts">
  import Window from '$lib/client/ui/window.svelte'

  const {
    ondismiss
  }: {
    ondismiss: () => void
  } = $props()
</script>

<Window {ondismiss} title="Logout Confirmation">
  <p>Are you sure you want to logout?</p>
</Window>
