<script lang="ts">
  import { useServerContext, type UserResource } from '$lib/client/client'
  import { page } from '$app/stores'
  import { derived, writable, type Writable } from 'svelte/store'
  import { onMount } from 'svelte'
  import { goto } from '$app/navigation'
  import Icon from '$lib/client/ui/icon.svelte'
  import { useAppContext } from '$lib/client/contexts/app'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import Separator from '$lib/client/ui/separator.svelte'

  const userId = derived(page, (page) => page.url.searchParams.get('id') || null)
  const { me, getUser } = useServerContext()
  const { isMobile } = useAppContext()

  const promise: Writable<Promise<UserResource | null> | null> = writable(null)

  $effect(() => {
    $promise =
      $userId != null
        ? (async (userId: string) => {
            if (userId == null) {
              await goto('/app/profile?id=' + (await me()).id, { replaceState: true })
            }

            const user = await getUser(userId)

            return user ?? null
          })($userId)
        : null
  })
</script>

{#if $promise != null}
  {#await $promise}
    <div class="loading">
      <LoadingSpinner size="3rem" />
    </div>
  {:then user}
    {#if user != null}
      <div class="page">
        <div class="header">
          <div class="user-info" class:mobile={$isMobile}>
            <div class="image">
              <Icon icon="user-circle" size="5rem" />
            </div>

            <div class="info">
              <h2 class="name">{user.displayName ?? `${user.firstName} ${user.lastName}`}</h2>
              <p class="username">@{user.username}</p>
            </div>
          </div>
        </div>

        <Separator  horizontal />
      </div>
    {/if}
  {/await}
{/if}

<style lang="scss">
  div.page {
    > div.header {
      > div.user-info {
        margin: 64px;
        gap: 32px;

        flex-direction: row;

        align-items: end;

        > div.info {
          > h2.name {
            font-size: 2em;
          }

          > p.username {
            font-weight: lighter;
          }
        }
      }

      > div.user-info.mobile {
        flex-direction: column;

        align-items: center;

        > div.info {
            align-items: center;
        }
      }
    }
  }

  div.loading {
    flex-grow: 1;

    align-items: center;
    justify-content: center;
  }
</style>
