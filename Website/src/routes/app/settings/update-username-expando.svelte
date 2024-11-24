<script lang="ts">
  import Input from '$lib/client/ui/input.svelte'
  import { derived, writable } from 'svelte/store'
  import Button from '$lib/client/ui/button.svelte'
  import { UsernameValidationFlags, useServerContext } from '$lib/client/client'
  import { onMount, type Snippet } from 'svelte'

  const { updateUsername, getUsernameValidationFlags } = useServerContext()

  const username = writable('')
  const errors = writable<string[]>([])
  const enable = derived(errors, (errors) => errors.length === 0)
  const submit = writable<() => void>(null as never)

  onMount(() => username.subscribe(() => validateUsername()))

  async function validateUsername() {
    const newErrors: string[] = []

    const flags = await getUsernameValidationFlags($username)

    if (flags & UsernameValidationFlags.TooShort) {
      newErrors.push('Username must be at least 6 characters long')
    }

    if (flags & UsernameValidationFlags.TooLong) {
      newErrors.push('Username must be at most 12 characters long')
    }

    if (flags & UsernameValidationFlags.InvalidChars) {
      newErrors.push('Username must only contain letters, numbers, and underscores')
    }

    $errors = newErrors
  }
</script>

<p>You can change your username.</p>

<Input
  type="text"
  id="username"
  name="New Username"
  bind:value={$username}
  onsubmit={() => $submit()}
/>

{#snippet foreground(view: Snippet)}
  <div class="foreground">
    {@render view()}
  </div>
{/snippet}

{#if $errors.length}
  {#each $errors as error}
    <p class="error">{error}</p>
  {/each}
{/if}

<Button
  bind:click={$submit}
  onclick={async () => {
    await updateUsername($username)
  }}
  disabled={!$enable}
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

    align-items: center;
    justify-content: center;
  }

  p.error {
    color: var(--color-6);
  }
</style>
