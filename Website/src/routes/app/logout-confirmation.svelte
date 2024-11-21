<script lang="ts">
  import Button from '$lib/client/ui/button.svelte'
  import Separator from '$lib/client/ui/separator.svelte'
  import Window from '$lib/client/ui/window.svelte'
  import { type Snippet } from 'svelte'

  const {
    ondismiss
  }: {
    ondismiss: () => void
  } = $props()
  import { useServerContext } from '$lib/client/client'
  const { deauthenticate } = useServerContext()
</script>

<Window {ondismiss} title="Logout Confirmation">
  <div class="body">
    <p>Are you sure you want to logout?</p>
  </div>

  <Separator horizontal />

  <div class="actions">
    {#snippet foreground(view: Snippet, primary: boolean = false)}
      <div class="foreground" class:primary>
        {@render view()}
      </div>
    {/snippet}

    {#snippet primaryForeground(view: Snippet)}
      {@render foreground(view, true)}
    {/snippet}

    <Button
      foreground={primaryForeground}
      onclick={async () => {
        await deauthenticate()
      }}>Yes</Button
    >
    <Button {foreground} onclick={ondismiss}>No</Button>
  </div>
</Window>

<style lang="scss">
  div.body {
    min-height: 32px;
    min-width: 360px;
  }

  div.actions {
    flex-direction: row;

    justify-content: flex-end;
  }

  div.foreground {
    padding: 8px 16px;
  }

  div.foreground.primary {
    background-color: var(--color-1);
    color: var(--color-5);
  }
</style>
