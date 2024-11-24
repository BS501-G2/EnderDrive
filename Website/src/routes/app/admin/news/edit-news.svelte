<script lang="ts">
  import Window from '$lib/client/ui/window.svelte'
  import Overlay from '../../../overlay.svelte'

  const { id, imageId, ondismiss }: { id: string | null; imageId: string; ondismiss: () => void } =
    $props()
</script>

<Window title="{id == null ? 'Create' : 'Edit'} news" {ondismiss}>
  <div class="edit-news"></div>
</Window>

<style lang="scss">
  div.edit-news {
    gap: 8px;
  }
</style>
