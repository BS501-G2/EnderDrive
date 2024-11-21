<script lang="ts">
  import Input from '$lib/client/ui/input.svelte'
  import Window from '$lib/client/ui/window.svelte'
  import { get, writable } from 'svelte/store'
  import Overlay from '../../overlay.svelte'
  import { useServerContext, type UserResource } from '$lib/client/client'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import Button from '$lib/client/ui/button.svelte'
  import { type Snippet } from 'svelte'
  import Icon from '$lib/client/ui/icon.svelte'

  const { ondismiss, onresult }: { ondismiss: () => void; onresult: (user: UserResource) => void } =
    $props()
  const { getUsers } = useServerContext()

  async function load(searchString: string): Promise<UserResource[]> {
    if (!searchString) {
      return []
    }

    return await getUsers(searchString)
  }

  const searchString = writable('')
  const promise = writable(load(''))

  $effect(() => {
    $promise = load($searchString)
  })
</script>

<Window {ondismiss} title="New Access">
  <Input id="name" type="text" name="Name" bind:value={$searchString} />

  <div class="result">
    {#if !$searchString}
      <div class="message">
        <Icon size="3rem" icon="magnifying-glass" thickness="solid" />
        <p>Type to search</p>
      </div>
    {:else}
      {#await $promise}
        <div class="message">
          <LoadingSpinner size="3rem" />
        </div>
      {:then users}
        {#if users.length === 0}
          <div class="message">
            <Icon size="3rem" icon="user" />
            <p class="no-results">No results</p>
          </div>
        {:else}
          {#each users as user}
            {#snippet foreground(view: Snippet)}
              <div class="user-container">
                {@render view()}
              </div>
            {/snippet}

            <Button {foreground} onclick={() => onresult(user)}>
              <div class="user-entry">
                <div class="icon">
                  <Icon size="1.2rem" icon="user" />
                </div>
                <div class="name">
                  <p class="name">{user.displayName ?? user.firstName}</p>
                  <p class="username">@{user.username}</p>
                </div>
              </div>
            </Button>
          {/each}
        {/if}
      {/await}
    {/if}
  </div>
</Window>

<style lang="scss">
  @use '../../../global.scss' as *;

  div.result {
    @include force-size(min(320px, 100dvw - 32px), min(360px, 100dvh - 32px));

    div.message {
      flex-grow: 1;

      align-items: center;
      justify-content: center;

      p.no-results {
        font-style: italic;
      }
    }

    overflow: hidden auto;

    div.user-container {
      flex-grow: 1;

      flex-direction: row;

      div.user-entry {
        flex-grow: 1;
        flex-direction: row;
        text-align: start;

        > div.icon {
          padding: 8px;
          align-items: center;
          justify-content: center;
        }

        > div.name {
          flex-grow: 1;

          > p.name {
            font-weight: bolder;
            font-size: 1.2rem;
          }

          > p.username {
            font-weight: lighter;
          }
        }
      }
    }
  }
</style>
