<script lang="ts">
  import { useServerContext, type PasswordResetRequestResource } from '$lib/client/client'
  import UserLink from '$lib/client/model/user-link.svelte'
  import Button from '$lib/client/ui/button.svelte'
  import { type Snippet } from 'svelte'
  import { writable } from 'svelte/store'
  import ResetDialog from './reset-dialog.svelte'

  const { reset, refresh }: { reset: PasswordResetRequestResource; refresh: () => void } = $props()
  const server = useServerContext()

  const openResetDialog = writable(false)
</script>

<div class="entry">
  <div class="username">
    <UserLink userId={reset.userId} />
  </div>

  <div class="actions">
    {#snippet foreground(view: Snippet)}
      <div class="foreground">
        {@render view()}
      </div>
    {/snippet}

    <Button
      {foreground}
      onclick={() => {
        $openResetDialog = true
      }}
    >
      Accept
    </Button>

    <Button
      {foreground}
      onclick={async () => {
        await server.declinePasswordResetRequest(reset.id)
        refresh()
      }}
    >
      Decline
    </Button>
  </div>
</div>
{#if $openResetDialog}
  <ResetDialog
    passwordResetId={reset.id}
    ondismiss={() => {
      $openResetDialog = false
    }}
    {refresh}
  />
{/if}

<style lang="scss">
  @use '../../../../global.scss' as *;

  div.entry {
    flex-direction: row;

    align-items: center;

    @include force-size(&, 32px);

    > div.username {
      flex-grow: 1;

      font-weight: bolder;
      font-size: 1.2rem;
    }

    > div.actions {
      flex-direction: row;

      div.foreground {
        padding: 8px;
      }
    }
  }
</style>
