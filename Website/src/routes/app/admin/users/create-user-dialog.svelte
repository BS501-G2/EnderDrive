<script lang="ts">
  import Button from '$lib/client/ui/button.svelte'
  import Input from '$lib/client/ui/input.svelte'
  import Window from '$lib/client/ui/window.svelte'
  import { type Snippet } from 'svelte'
  import { derived, writable } from 'svelte/store'
  // import { UsernameValidationFlags, useServerContext } from '$lib/client/client'
  import { UsernameValidationFlags } from '$lib/client/resource'
  import { useClientContext } from '$lib/client/client'

  const {
    ondismiss,
    onresult
  }: { ondismiss: () => void; onresult: (output: { password: string; userId: string }) => void } =
    $props()

  const { server } = useClientContext()

  const username = writable<string>('')
  const firstName = writable<string>('')
  const middleName = writable<string>('')
  const lastName = writable<string>('')
  const displayName = writable<string>('')

  const usernameError = writable<string[] | null>(null)
  const firstNameError = writable<string[] | null>(null)
  const middleNameError = writable<string[] | null>(null)
  const lastNameError = writable<string[] | null>(null)
  const displayNameError = writable<string[] | null>(null)

  async function onsubmit(): Promise<void> {
    $usernameError = null
    $firstNameError = null
    $middleNameError = null
    $lastNameError = null
    $displayNameError = null

    const flags = await server.GetUsernameValidationFlags({ Username: $username })

    if (flags !== UsernameValidationFlags.OK) {
      const errors: string[] = []
      if (flags & UsernameValidationFlags.TooShort) {
        errors.push('Username must be at least 6 characters long')
      }

      if (flags & UsernameValidationFlags.TooLong) {
        errors.push('Username must be at most 12 characters long')
      }

      if (flags & UsernameValidationFlags.InvalidChars) {
        errors.push('Username must only contain letters, numbers, and underscores')
      }

      $usernameError = errors
    }

    if (!$firstName) {
      $firstNameError = ['First name is required']
    }

    if (!$lastName) {
      $lastNameError = ['Last name is required']
    }

    if (
      $usernameError == null &&
      $firstNameError == null &&
      $middleNameError == null &&
      $lastNameError == null &&
      $displayNameError == null
    ) {
      const { Password, UserId } = await server.CreateUser({
        Username: $username,
        FirstName: $firstName,
        MiddleName: $middleName,
        LastName: $lastName,
        DisplayName: $displayName
      })

      onresult({ password: Password, userId: UserId })
    }
  }

  const buttonElement = writable<() => void>(null as never)
</script>

{#snippet error(error: string[] | null)}
  {#if error != null && error.length > 0}
    {#each error as a}
      <p class="error">{a}</p>
    {/each}
  {/if}
{/snippet}

<Window titleIcon={{ icon: 'user-plus', thickness: 'solid' }} title="Create New User" {ondismiss}>
  <div class="create-dialog">
    <div class="panel">
      <h2>Account Information</h2>
      <div class="fields">
        <Input type="text" id="username" name="Username" bind:value={$username} />
        {@render error($usernameError)}
        <Input type="text" id="display-name" name="Display Name" bind:value={$displayName} />
        {@render error($displayNameError)}
      </div>
    </div>
    <div class="panel">
      <h2>Personal Information</h2>
      <div class="fields">
        <Input type="text" id="first-name" name="First Name" bind:value={$firstName} />
        {@render error($firstNameError)}
        <Input type="text" id="middle-name" name="Middle Name" bind:value={$middleName} />
        {@render error($middleNameError)}
        <Input type="text" id="last-name" name="Last Name" bind:value={$lastName} />
        {@render error($lastNameError)}
      </div>
    </div>

    {#snippet foreground(view: Snippet)}
      <div class="foreground">
        {@render view()}
      </div>
    {/snippet}

    <Button {foreground} bind:click={$buttonElement} onclick={onsubmit}>
      <p>Create New User</p>
    </Button>
  </div>
</Window>

<style lang="scss">
  @use '../../../../global.scss' as *;

  div.create-dialog {
    gap: 8px;

    > div.panel {
      gap: 8px;

      > h2 {
        font-size: 1.2rem;
      }

      > div.fields {
        gap: 8px;

        > div.field {
          flex-direction: row;
        }
      }
    }

    div.foreground {
      flex-grow: 1;

      padding: 8px;
      align-items: center;
      justify-content: center;

      background-color: var(--color-1);
      color: var(--color-5);
    }
  }

  p.error {
    color: var(--color-6);
  }
</style>
