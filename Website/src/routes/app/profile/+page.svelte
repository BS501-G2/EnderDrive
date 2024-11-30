<script lang="ts">
  import { useClientContext } from '$lib/client/client'
  import { page } from '$app/stores'
  import { derived, writable, type Writable } from 'svelte/store'
  import { onMount } from 'svelte'
  import { goto } from '$app/navigation'
  import Icon from '$lib/client/ui/icon.svelte'
  import { useAppContext } from '$lib/client/contexts/app'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import Separator from '$lib/client/ui/separator.svelte'
  import { createUserContext } from './user-context'
  import UserTabs from './user-tabs.svelte'
  import UserTab from './user-tab.svelte'
  import SharedWithMe from './shared-with-me.svelte'
  import AboutMe from './about-me.svelte'

  const userId = derived(page, (page) => page.url.searchParams.get('id') || null)
  const { server } = useClientContext()
  const { isMobile } = useAppContext()
  const { tabs, currentTabIndex } = createUserContext()
</script>

{#await (async (userId) => {
  const me = await server.Me({})
  const user = await server.GetUser({ UserId: userId ?? me.Id })

  return { user, me: me }
})($userId)}
  <div class="loading">
    <LoadingSpinner size="3rem" />
  </div>
{:then { user, me }}
  {#if user != null}
    <div class="page">
      <div class="header">
        <div class="user-info" class:mobile={$isMobile}>
          <div class="image">
            <Icon icon="user-circle" size="5rem" />
          </div>

          <div class="info">
            <h2 class="name">{user.DisplayName ?? `${user.FirstName} ${user.LastName}`}</h2>
            <p class="username">@{user.Username}</p>
          </div>
        </div>
      </div>

      <Separator horizontal />

      <UserTabs {tabs} {currentTabIndex} />

      <UserTab
        label="Shared {user.Id === me.Id ? 'Files' : 'With You'}"
        icon={{ icon: 'users', thickness: 'solid' }}
      >
        <SharedWithMe {user} />
      </UserTab>

      <UserTab
        label="About {user.Id === me.Id ? 'You' : 'Me'}"
        icon={{ icon: 'info', thickness: 'solid' }}
      >
        <AboutMe {user} {me} />
      </UserTab>

      <Separator horizontal />

      <div class="main-container">
        <div class="main">
          {@render $tabs[$currentTabIndex]?.snippet()}
        </div>
      </div>
    </div>
  {/if}
{/await}

<style lang="scss">
  div.page {
    flex-grow: 1;

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

    div.main-container {
      flex-grow: 1;

      padding: 8px 64px;

      min-width: 0;
      min-height: 0;

      > div.main {
        flex-grow: 1;

        overflow: auto;

        min-width: 0;
        min-height: 0;
      }
    }
  }

  div.loading {
    flex-grow: 1;

    align-items: center;
    justify-content: center;
  }
</style>
