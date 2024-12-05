<script lang="ts">
  import { onMount } from 'svelte'
  import { useClientContext } from '../client'
  import LoadingSpinner from '../ui/loading-spinner.svelte'
  import { writable } from 'svelte/store'
  import type { UserResource } from '../resource'
  const {
    userId,
    customText,
    noclickable = false
  }: {
    userId: string
    customText?: string

    noclickable?: boolean
  } = $props()
  const { server } = useClientContext()

  async function load(): Promise<UserResource | null> {
    const user = await server.GetUser({ UserId: userId })

    return user!
  }

  const promise = writable(load())
</script>

{#await $promise}
  <LoadingSpinner size="1rem" />
{:then user}
  {#if user}
    <a
      class="user"
      class:clickable={!noclickable}
      href={!noclickable ? `/app/profile?id=${user.Id}` : '#'}
      onclick={({ preventDefault }) => {
        if (noclickable) {
          preventDefault()
        }
      }}
    >
      {customText ?? user.DisplayName ?? `${user.FirstName}`}
    </a>
    
  {:else}
    <p class="invalid">Invalid User ID</p>
  {/if}
{:catch error}
  <p class="invalid">
    {error.message}
  </p>
{/await}

<style lang="scss">
  a.user {
    color: inherit;
    text-decoration: none;
    // font-weight: bolder;
  }

  a.user.clickable:hover {
    text-decoration: underline;
  }

  a.user:visited {
    color: inherit;
  }

  p.invalid {
    color: red;
  }
</style>
