<script lang="ts">
  import { type UserResource } from '$lib/client/resource';
  import Window from '$lib/client/ui/window.svelte'

  const { user, ondismiss }: { user: UserResource, ondismiss: () => void } = $props()
</script>

<Window {ondismiss}>
    <div class="role-editor">

    </div>
</Window>

<style lang="scss">

</style>
