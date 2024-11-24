<script lang="ts">
  import { PasswordValidationFlags, useServerContext } from '$lib/client/client'
  import Button from '$lib/client/ui/button.svelte'
  import Input from '$lib/client/ui/input.svelte'
  import { onMount, type Snippet } from 'svelte'
  import { derived, writable } from 'svelte/store'

  const { updatePassword, getPasswordValidationFlags } = useServerContext()
  const server = useServerContext()

  const currentPassword = writable('')
  const newPassword = writable('')
  const confirmPassword = writable('')
  const errors = writable<string[]>([])
  const enable = derived(errors, (errors) => errors.length === 0)
  const submit = writable<() => void>(null as never)

  onMount(() => currentPassword.subscribe(() => validatePassword()))
  onMount(() => newPassword.subscribe(() => validatePassword()))
  onMount(() => confirmPassword.subscribe(() => validatePassword()))

  async function validatePassword() {
    const newErrors: string[] = []

    const flags = await getPasswordValidationFlags($newPassword, $confirmPassword)

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
</script>

<Input type="password" id="username" name="Current Password" bind:value={$currentPassword} />
<Input type="password" id="password" name="New Password" bind:value={$newPassword} />
<Input type="password" id="confirm-password" name="Confirm New Password" bind:value={$confirmPassword} />

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

<Button {foreground} disabled={$enable} bind:click={$submit} onclick={async () => {

}}>
  Update Password
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
