<script lang="ts">
  import { useClientContext } from '$lib/client/client'
  import Button from '$lib/client/ui/button.svelte'
  import Input from '$lib/client/ui/input.svelte'
  import Window from '$lib/client/ui/window.svelte'
  import { type Snippet } from 'svelte'
  import { writable } from 'svelte/store'

  const { ondismiss }: { ondismiss: () => void } = $props()
  const { server } = useClientContext()
  const text = writable<string>('')

  const submitButton = writable<() => void>(null as never)
  const success = writable(false)
</script>

{#snippet foreground(view: Snippet)}
  <div class="foreground">
    {@render view()}
  </div>
{/snippet}

{#snippet background(view: Snippet)}
  <div class="background">
    {@render view()}
  </div>
{/snippet}
<Window {ondismiss} title="Request Password Reset">
  <div class="body">
    <p>Please enter your usernam and wait for an administrator will accept your request.</p>

    <Input
      type="text"
      bind:value={$text}
      id="username"
      name="Username"
      onsubmit={() => $submitButton()}
    />

    <Button
      {foreground}
      {background}
      bind:click={$submitButton}
      onclick={async () => {
        await server.RequestPasswordReset({ Username: $text })

        $success = true
      }}
    >
      Submit
    </Button>
  </div>
</Window>

{#if $success}
  <Window title="Success" {ondismiss}>
    <div class="success">Please wait for the administrator to accept your password request.</div>
  </Window>
{/if}

<style lang="scss">
  @use '../../global.scss' as *;

  div.body {
    gap: 8px;

    @include force-size(430px, &);
  }

  div.background {
    background-color: var(--color-1);
    color: var(--color-5);
  }

  div.foreground {
    flex-grow: 1;

    padding: 8px;
  }
</style>
