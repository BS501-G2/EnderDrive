<script lang="ts">
  import { goto } from '$app/navigation'
  import { useClientContext, UserRole, useServerContext } from '$lib/client/client'
  import Button from '$lib/client/ui/button.svelte'
  import RequireClient from '$lib/client/ui/require-client.svelte'
  import { onMount, type Snippet } from 'svelte'
  import { writable } from 'svelte/store'

  const actions = writable<
    {
      id: number
      name: string
      onClick: () => Promise<any>
    }[]
  >([])

  const output = writable<any>(null)

  const pushAction = (name: string, onClick: () => Promise<any>) => {
    const id = Math.random()

    $actions = [
      ...$actions,
      {
        id,
        name,
        onClick: async () => {
          try {
            const result = await onClick()

            return ($output = typeof result === 'string' ? result : JSON.stringify(result))
          } catch (error: any) {
            $output = `${error.stack}`

            throw error
          }
        }
      }
    ]

    return () => {
      $actions = $actions.filter((action) => action.id !== id)
    }
  }

  const adminUsername = 'testuser'
  const adminPassword = 'TestUser123;'

  const {
    getSetupRequirements,
    createAdmin,
    me,
    deauthenticate,
    createUser,
    resolveUsername,
    authenticatePassword
  } = useServerContext()

  const { authentication } = useClientContext()

  onMount(() => pushAction('Get Setup Requirements', () => getSetupRequirements()))

  onMount(() => pushAction('Go To Landing', () => goto('/landing')))

  onMount(() => pushAction('Go To App', () => goto('/app/files')))

  onMount(() => pushAction('Resolve Username', async () => resolveUsername(adminUsername)))

  onMount(() =>
    pushAction('Create Administrator', async () => {
      await createAdmin(
        adminUsername,
        adminPassword,
        adminPassword,
        'Admin',
        'Nis',
        'Trator',
        'Administrator'
      )
    })
  )

  onMount(() =>
    pushAction('Login As Administrator', async () => {
      const userId = await resolveUsername(adminUsername)

      if (userId == null) {
        throw new Error('User not found.')
      }

      await authenticatePassword(userId, adminPassword)
    })
  )

  onMount(() => pushAction('Logout', async () => deauthenticate()))

  onMount(() =>
    pushAction('Create Test Users', async () => {
      for (let iteration = 0; iteration < 10; iteration++) {
        await createUser({
          username: `testuser${iteration}`,
          password: 'TestUser123;',
          firstName: 'Test',
          lastName: 'User'
        })
      }
    })
  )

  for (let iteration = 0; iteration < 10; iteration++) {
    const capturedIteration = iteration

    onMount(() =>
      pushAction(`Login As User ${capturedIteration}`, async () => {
        await authenticatePassword(
          (await resolveUsername(`testuser${capturedIteration}`))!,
          'TestUser123;'
        )
      })
    )
  }
</script>

{#snippet background(view: Snippet)}
  <div class="button">
    {@render view()}
  </div>
{/snippet}

{#snippet foreground(view: Snippet)}
  <p class="button">
    {@render view()}
  </p>
{/snippet}

<RequireClient nosetup>
  <div class="card">
    <div class="header">
      <h2>Actions</h2>

      <div>
        <p>Current Account</p>

        {#if $authentication != null}
          {#await me() then myAccount}
            <p>{myAccount.displayName ?? myAccount.firstName} (@{myAccount.username})</p>
          {/await}
        {:else}
          (none)
        {/if}
      </div>
    </div>
    <div class="separator"></div>
    <div class="body actions">
      {#each $actions as { id, name, onClick } (id)}
        <Button {background} onclick={onClick} {foreground}>{name}</Button>
      {/each}
    </div>
  </div>

  <div class="card">
    <div class="header">
      <h2>Output</h2>
    </div>
    <div class="separator"></div>
    <div class="body">
      <pre>{output}</pre>
    </div>
  </div>
</RequireClient>

<style lang="scss">
  @use '../../global.scss' as *;

  div.button {
    background-color: var(--color-5);
  }

  p.button {
    padding: 8px;
  }

  div.card {
    padding: 16px;
    margin: 16px;
    gap: 16px;

    > div.header {
      flex-direction: row;
      align-items: flex-end;

      > h2 {
        flex-grow: 1;
      }
    }

    > div.separator {
      background-color: var(--color-1);

      @include force-size(&, 1px);
    }

    > div.body.actions {
      flex-direction: row;
      flex-wrap: wrap;

      gap: 8px;
    }
  }

  pre {
    font: revert;
  }
</style>
