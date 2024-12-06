<script lang="ts">
  import { useClientContext } from '$lib/client/client'
  import { type GoogleAccountInformation } from '$lib/client/server-functions'
  import Button from '$lib/client/ui/button.svelte'
  import { type Snippet } from 'svelte'
  import GoogleButton from './google-button.svelte'

  const { server } = useClientContext()

  let refreshKey: {} = $state({})
</script>

{#snippet current(current: GoogleAccountInformation | null)}
  <div class="current">
    <p>
      Current Google Account:
      {#if current == null}
        <span class="google-info">None</span>
      {:else}
        <span class="google-info">{current.Name} ({current.Email})</span>
      {/if}
    </p>
  </div>
{/snippet}

{#key refreshKey}
  {#await server.GetGoogleAuthentication({}) then googleAccountInfo}
    {@render current(googleAccountInfo)}

    {#if googleAccountInfo == null}
      <GoogleButton
        refresh={() => {
          refreshKey = {}
        }}
      />
    {:else}
      {#snippet foreground(view: Snippet)}
        <div class="foreground">
          {@render view()}
        </div>
      {/snippet}

      <Button
        {foreground}
        onclick={async () => {
          await server.SetGoogleAuthentication({ Token: null })
          refreshKey = {}
        }}
      >
        Unbind Google Account
      </Button>
    {/if}
  {/await}
{/key}

<style lang="scss">
  span.google-info {
    font-weight: bolder;
  }

  div.foreground {
    flex-grow: 1;
    padding: 8px;

    border: solid 1px var(--color-1);
    color: var(--color-1);
  }
</style>
