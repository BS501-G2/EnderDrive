<script lang="ts">
  import { useDashboardContext } from '$lib/client/contexts/dashboard'
  import Button from '$lib/client/ui/button.svelte'
  import Icon from '$lib/client/ui/icon.svelte'
  import { onMount, type Snippet } from 'svelte'

  const { install }: { install: () => void } = $props()
  const { pushDesktopTopRight } = useDashboardContext()

  onMount(() => pushDesktopTopRight(a))
</script>

{#snippet a()}
  {#snippet buttonForeground(view: Snippet)}
    <div class="button-foreground">
      {@render view()}
    </div>
  {/snippet}

  <div class="notification">
    <Button foreground={buttonForeground} onclick={() => install()}>
      <Icon icon="download" thickness="solid" />
    </Button>
  </div>
{/snippet}

<style lang="scss">
  div.notification {
    -webkit-app-region: no-drag;

    flex-direction: row;

    align-items: center;
  }

  div.button-foreground {
    padding: 8px;
  }
</style>
