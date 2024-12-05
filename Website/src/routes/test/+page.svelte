<script lang="ts">
  import { goto } from '$app/navigation'
  import { useClientContext } from '$lib/client/client'
  import Button from '$lib/client/ui/button.svelte'
  import RequireClient from '$lib/client/ui/require-client.svelte'
  import { onMount, type Snippet } from 'svelte'
  import { writable } from 'svelte/store'
  import { uniqueNamesGenerator, names } from 'unique-names-generator'

  const actions = writable<
    {
      id: number
      name: string
      onClick: () => Promise<any>
    }[]
  >([])

  const output = writable<any>(null)

  output.subscribe(console.log)

  const randomName = () =>
    uniqueNamesGenerator({
      dictionaries: [names]
    })

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

            return ($output =
              typeof result === 'string' ? result : JSON.stringify(result, void 0, '  '))
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

  const { authentication, server } = useClientContext()

  onMount(() => pushAction('Get Setup Requirements', () => server.SetupRequirements({})))

  onMount(() => pushAction('Go To Landing', () => goto('/landing')))

  onMount(() => pushAction('Go To App', () => goto('/app/files')))

  onMount(() =>
    pushAction('Resolve Username', async () => server.ResolveUsername({ Username: adminUsername }))
  )

  onMount(() =>
    pushAction('Create Administrator', async () => {
      await server.CreateAdmin({
        Username: adminUsername,
        Password: adminPassword,
        ConfirmPassword: adminPassword,
        FirstName: randomName(),
        LastName: randomName(),
        MiddleName: randomName()
      })
    })
  )

  onMount(() =>
    pushAction('Login As Administrator', async () => {
      const userId = await server.ResolveUsername({ Username: adminUsername })

      if (userId == null) {
        throw new Error('User not found.')
      }

      await server.AuthenticatePassword({ UserId: userId, Password: adminPassword })
    })
  )

  onMount(() => pushAction('Logout', async () => server.Deauthenticate({})))

  onMount(() =>
    pushAction('Create Test Users', async () => {
      const promises: Promise<{
        UserId: string
        Password: string
      }>[] = []

      for (let iteration = 0; iteration < 10; iteration++) {
        promises.push(
          server.CreateUser({
            Username: `testuser${iteration}`,
            Password: 'TestUser123;',
            FirstName: randomName(),
            LastName: randomName(),
        MiddleName: randomName()
          })
        )
      }

      await Promise.all(promises)
    })
  )

  for (let iteration = 0; iteration < 10; iteration++) {
    const capturedIteration = iteration

    onMount(() =>
      pushAction(`Login As User ${capturedIteration}`, async () => {
        await server.AuthenticatePassword({
          UserId: (await server.ResolveUsername({ Username: `testuser${capturedIteration}` }))!,
          Password: 'TestUser123;'
        })
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

<div class="page">
  <RequireClient nosetup>
    <div class="card">
      <div class="header">
        <h2>Actions</h2>

        <div>
          <p>Current Account</p>
          {#if $authentication != null}
            {#await server.Me({}) then me}
              <p>{me.DisplayName ?? me.FirstName} (@{me.Username})</p>
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

    <div class="card extend">
      <div class="header">
        <h2>Output</h2>
      </div>
      <div class="separator"></div>
      <div class="body">
        <pre>{$output}</pre>
      </div>
    </div>
  </RequireClient>
</div>

<style lang="scss">
  @use '../../global.scss' as *;

  div.page {
    @include force-size(100dvw, 100dvh);
  }

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

    > div.body {
      flex-grow: 1;
    }

    > div.body.actions {
      flex-direction: row;
      flex-wrap: wrap;

      gap: 8px;
    }
  }

  div.card.extend {
    flex-grow: 1;
  }

  pre {
    flex-grow: 1;
    min-width: 0;

    font: revert;

    overflow: auto hidden;
  }
</style>
