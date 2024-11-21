<script lang="ts">
  import { useFileBrowserPropertiesContext } from '$lib/client/contexts/file-browser-properties'
  import type { IconOptions } from '$lib/client/ui/icon.svelte'
  import type { Snippet } from 'svelte'

  const { pushTab } = useFileBrowserPropertiesContext()

  const { label, icon, children }: { label: string; icon: IconOptions; children?: Snippet } =
    $props()

  $effect(() => {
    if (children != null) {
      return pushTab(label, icon, children)
    }
  })
</script>
