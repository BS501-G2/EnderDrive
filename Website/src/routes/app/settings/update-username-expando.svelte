<script lang="ts">
  import Input from '$lib/client/ui/input.svelte'
  import { writable } from 'svelte/store'
  import AccountSettingsTab from './account-settings-tab.svelte'
  import Expando from './expando.svelte'
  import Button from '$lib/client/ui/button.svelte'
  import { useServerContext } from '$lib/client/client'
  import { type Snippet } from 'svelte'

  const username = writable('')
  const { updateUsername } = useServerContext()

  const a = writable<() => void>(null as never)
</script>

<p>You can change your username.</p>

<Input type="text" id="username" name="New Username" bind:value={$username} onsubmit={() => $a()} />

{#snippet foreground(view: Snippet)}
  <div class="foreground">
    {@render view()}
  </div>
{/snippet}

<Button
  bind:click={$a}
  onclick={async () => {
    await updateUsername($username)
  }}
  {foreground}
>
  <p>Update Username</p>
</Button>

<style lang="scss">
  div.foreground {
    flex-grow: 1;

    padding: 8px;

    background-color: var(--color-1);
    color: var(--color-5);
  }
</style>
