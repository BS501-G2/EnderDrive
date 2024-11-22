<script lang="ts">
  import { onMount } from 'svelte'
  import { useServerContext, type UserResource } from '../client'
  import LoadingSpinner from '../ui/loading-spinner.svelte'
  import { writable } from 'svelte/store'

  const {
    userId,
    customText
  }: {
    userId: string
    customText?: string
  } = $props()
  const { getUser } = useServerContext()

  async function load(): Promise<UserResource | null> {
    const user = getUser(userId)

    return user
  }

  const promise = writable(load())
</script>

{#await $promise}
  <LoadingSpinner size="1rem" />
{:then user}
  {#if user}
    <a class="user" href="/app/profile?id={user.id}">
      {customText ?? user.displayName ?? `${user.firstName}`}
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

  a.user:hover {
    text-decoration: underline;
  }

  a.user:visited {
    color: inherit;
  }

  p.invalid {
    color: red;
  }
</style>
