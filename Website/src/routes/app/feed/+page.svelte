<script lang="ts">
  import { useServerContext } from '$lib/client/client'
  import { useAppContext } from '$lib/client/contexts/app'
  import Button from '$lib/client/ui/button.svelte'
  import Separator from '$lib/client/ui/separator.svelte'
  import { type Snippet } from 'svelte'
  import SharedFiles from './shared-files.svelte'
  import { goto } from '$app/navigation'
  import RecentFiles from './recent-files.svelte'
  const {} = $props()
  const { me } = useServerContext()
  const { isDesktop } = useAppContext()
</script>

{#await me() then user}
  {#if $isDesktop}
    <div class="page">
      <div class="welcome">
        <h2>
          Welcome,
          {user!.firstName}!
        </h2>
      </div>

      <Separator horizontal />

      {@render content()}
    </div>
  {:else}
    {@render content()}
  {/if}
{/await}

{#snippet content()}
  <div class="shared section">
    <div class="header">
      <h2 class="title">Shared Files</h2>

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
        onclick={() => goto('/app/shared')}
      >
        See More
      </Button>
    </div>

    <Separator horizontal />

    <SharedFiles />
  </div>

  <div class="recent section">
    <div class="header">
      <h2 class="title">Recent Files</h2>

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
        onclick={() => goto('/app/files')}
      >
        See More
      </Button>
    </div>

    <Separator horizontal />

    <RecentFiles />
  </div>
{/snippet}

<style lang="scss">
  div.page {
    margin: 64px;
    gap: 8px;

    > div.welcome {
      // font-weight: bolder;

      font-size: 2rem;
    }
  }

  div.section {
    padding: 16px;
    gap: 8px;

    > div.header {
      flex-direction: row;
      align-items: center;

      > h2.title {
        flex-grow: 1;

        font-size: x-large;
      }

      div.recent-background {
        background-color: var(--color-5);
        color: var(--color-1);
      }

      div.recent-foreground {
        padding: 8px;
      }
    }
  }
</style>
