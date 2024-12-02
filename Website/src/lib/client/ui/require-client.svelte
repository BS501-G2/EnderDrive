<script lang="ts">
  import { onMount, type Snippet } from 'svelte'
  import { useClientContext } from '../client'
  import Button from './button.svelte'
  import LoadingSpinner from './loading-spinner.svelte'
  import Banner from './banner.svelte'
  import { goto } from '$app/navigation'
  import { page } from '$app/stores'

  const { clientState, server } = useClientContext()

  const {
    children,
    nosetup = false
  }: {
    children: Snippet
    nosetup?: boolean
  } = $props()

  onMount(async () => {
    const response = await server.SetupRequirements({})

    if (response.AdminSetupRequired && !nosetup) {
      await goto(
        `/setup?return=${encodeURIComponent(`${$page.url.pathname}${$page.url.search}`)}`,
        {
          replaceState: true
        }
      )
    }
  })
</script>

{#if $clientState[0] === 'connecting'}
  <div class="splash-container">
    <div class="splash">
      <LoadingSpinner size="3rem" />
    </div>
  </div>
{:else if $clientState[0] === 'connnected'}
  {@render children()}
{:else if $clientState[0] === 'disconnected'}
  <div class="splash-container error">
    <div class="splash">
      <Banner
        type="error"
        icon={{
          icon: 'xmark',
          thickness: 'solid',
          size: '1.5em'
        }}
      >
        {#snippet children()}
          <p>
            {$clientState[1] ?? 'Unknown error'}
          </p>
        {/snippet}

        {#snippet bottom()}
          {#snippet container(view: Snippet)}
            <div class="retry-button">
              {@render view()}
            </div>
          {/snippet}
          <Button background={container} onclick={() => $clientState[2]()}>Retry</Button>
        {/snippet}
      </Banner>
    </div>
  </div>
{/if}

<style lang="scss">
  div.splash-container {
    flex-grow: 1;

    display: flex;

    align-items: center;
    justify-content: center;
  }

  div.splash {
    flex-direction: column;
    align-items: center;
    justify-content: center;

    gap: 8px;
  }

  div.retry-button {
    padding: 8px 16px;

    border: solid 1px var(--color-1);
  }
</style>
