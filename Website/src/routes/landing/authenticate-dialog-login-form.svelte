<script lang="ts">
  import { useServerContext } from '$lib/client/client'
  import Button from '$lib/client/ui/button.svelte'
  import Input from '$lib/client/ui/input.svelte'
  import { onMount, type Snippet } from 'svelte'
  import { writable } from 'svelte/store'
  import AuthenticateDialogLoginForms from './authenticate-dialog-login-actions.svelte'

  const { redirect, ondismiss, onreset }: { redirect: () => Promise<void>, ondismiss: () => void, onreset: ()=> void } = $props()

  const username = writable<string>('')
  const password = writable<string>('')

  const { authenticatePassword, resolveUsername } = useServerContext()

  async function onclick() {
    const userId = await resolveUsername($username)

    if (userId == null) {
      throw new Error('Inavlid username or password.')
    }

    await authenticatePassword(userId, $password)
    ondismiss()
    await redirect()
  }

  const click = writable(() => {})
  const reset = writable(() => {})

  onMount(() => username.subscribe(() => $reset()))
  onMount(() => password.subscribe(() => $reset()))
</script>

<div class="field">
  <Input
    icon={{
      icon: 'user',
      thickness: 'solid'
    }}
    id="username"
    type="text"
    name="Username"
    bind:value={$username}
    onsubmit={$click}
  />

  <Input
    icon={{
      icon: 'key',
      thickness: 'solid'
    }}
    id="password"
    type="password"
    name="Password"
    bind:value={$password}
    onsubmit={$click}
  />

  {#snippet loginBackground(view: Snippet)}
    <div class="submit-outer">
      {@render view()}
    </div>
  {/snippet}

  {#snippet loginForeground(view: Snippet)}
    <div class="submit">
      {@render view()}
    </div>
  {/snippet}

  <Button
    background={loginBackground}
    foreground={loginForeground}
    bind:reset={$reset}
    bind:click={$click}
    {onclick}
  >
    <p>Login</p>
  </Button>
</div>

<div class="choices">
  <div class="or">
    <div class="line"></div>
    <p>or</p>
    <div class="line"></div>
  </div>

  <AuthenticateDialogLoginForms {onreset} />
</div>

<style lang="scss">
  @use '../../global.scss' as *;

  div.field {
    gap: 16px;

    justify-content: safe center;

    div.submit-outer {
      background-color: var(--color-1);
      color: var(--color-5);
      flex-grow: 1;
    }

    div.submit {
      min-height: 32px;

      align-items: center;
      justify-content: center;
    }
  }

  div.choices {
    gap: 16px;

    > div.or {
      flex-direction: row;
      align-items: center;

      gap: 8px;

      div.line {
        flex-grow: 1;

        @include force-size(&, 1px);

        background-color: var(--color-1);
      }
    }
  }
</style>
