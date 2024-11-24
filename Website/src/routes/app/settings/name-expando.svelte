<script lang="ts">
  import { goto } from '$app/navigation'
  import { useServerContext, type UserResource } from '$lib/client/client'
  import Button from '$lib/client/ui/button.svelte'
  import Input from '$lib/client/ui/input.svelte'
  import { onMount, type Snippet } from 'svelte'
  import { writable } from 'svelte/store'

  const firstName = writable('')
  const middleName = writable('')
  const lastName = writable('')
  const displayName = writable('')

  const errors = writable<string[]>([])

  function check() {
    const newErrors: string[] = []

    if (!$firstName.length) {
      newErrors.push('First name must not be empty.')
    }

    if (!$lastName.length) {
      newErrors.push('Last name must not be empty.')
    }

    errors.set(newErrors)
  }

  onMount(() => firstName.subscribe(() => check()))
  onMount(() => middleName.subscribe(() => check()))
  onMount(() => lastName.subscribe(() => check()))
  onMount(() => displayName.subscribe(() => check()))

  const server = useServerContext()

  const { me }: { me: UserResource } = $props()

  onMount(() => {
    $firstName = me.firstName
    $middleName = me.middleName || ''
    $lastName = me.lastName
    $displayName = me.displayName || ''
  })
</script>

<Input name="First Name" type="text" id="username" bind:value={$firstName} />
<Input name="Middle Name" type="text" id="middleName" bind:value={$middleName} />
<Input name="Last Name" type="text" id="lastName" bind:value={$lastName} />
<Input name="Display Name" type="text" id="displayName" bind:value={$displayName} />

{#snippet recentForeground(view: Snippet)}
  <div class="recent-foreground">
    {@render view()}
  </div>
{/snippet}
{#snippet recentBackground(view: Snippet)}
  <div class="recent-background">
    {@render view()}
  </div>
{/snippet}

<Button
  foreground={recentForeground}
  background={recentBackground}
  onclick={async () => {
    await server.updateName({
      firstName: $firstName,
      middleName: $middleName || null,
      lastName: $lastName,
      displayName: $displayName || null
    })
  }}
>
  Update
</Button>

<style lang="scss">
  div.recent-background {
    background-color: var(--color-1);
    color: var(--color-5);
  }

  div.recent-foreground {
    padding: 8px;
  }
</style>
