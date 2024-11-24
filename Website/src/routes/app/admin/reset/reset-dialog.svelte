<script lang="ts">
  import { PasswordValidationFlags, useServerContext } from '$lib/client/client'
  import Button from '$lib/client/ui/button.svelte'
  import Input from '$lib/client/ui/input.svelte'
  import Window from '$lib/client/ui/window.svelte'
  import { onMount, type Snippet } from 'svelte'
  import { writable } from 'svelte/store'

  const { passwordResetId, ondismiss, refresh }: { passwordResetId: string; ondismiss: () => void, refresh:() => void } =
    $props()

  const server = useServerContext()

  const errors = writable<string[]>([])

  const password = writable<string>('')
  const confirm = writable<string>('')

  async function check() {
    const newErrors: string[] = []

    const flags = await server.getPasswordValidationFlags($password, $confirm)

    if (flags & PasswordValidationFlags.TooShort) {
      newErrors.push('Password is too short')
    }

    if (flags & PasswordValidationFlags.TooLong) {
      newErrors.push('Password is too long')
    }

    if (flags & PasswordValidationFlags.NoRequiredChars) {
      newErrors.push(
        'Password must contain at least one small letter, one capital letter, one number, and one special character'
      )
    }

    if (flags & PasswordValidationFlags.PasswordMismatch) {
      newErrors.push('Passwords do not match')
    }

    errors.set(newErrors)
  }

  onMount(() => password.subscribe(() => check()))
  onMount(() => confirm.subscribe(() => check()))
</script>

{#snippet foreground(view: Snippet)}
  <div class="foreground">
    {@render view()}
  </div>
{/snippet}

<Window {ondismiss} title="Reset New Password">
  <div class="body">
    <Input type="password" id="password" bind:value={$password} name="Password" />
    <Input type="password" id="confirm-password" bind:value={$confirm} name="Confirm Password" />

    {#if $errors.length}
      {#each $errors as error}
        <p class="error">{error}</p>
      {/each}
    {/if}

    <Button
      {foreground}
      disabled={$errors.length > 0}
      onclick={async () => {
        await server.acceptPasswordResetRequest(passwordResetId, $password)
        refresh()
      }}
    >
      Accept
    </Button>
  </div>
</Window>

<style lang="scss">
  @use '../../../../global.scss' as *;

  div.body {
    @include force-size(320px, &);

    gap: 8px;
  }
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
