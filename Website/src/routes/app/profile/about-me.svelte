<script lang="ts">
  import { goto } from '$app/navigation'
  import { useServerContext, type UserResource } from '$lib/client/client'
  import Button from '$lib/client/ui/button.svelte'
  import Separator from '$lib/client/ui/separator.svelte'
  import { type Snippet } from 'svelte'
  const { user, me }: { user: UserResource; me: UserResource } = $props()

  const server = useServerContext()
</script>

<h2 class="title">About {user.id === me.id ? 'You' : 'Me'}</h2>

{#snippet row(label: string, value: string | null)}
  <div class="row">
    <p class="label">{label}</p>
    <Separator vertical />
    <p class="value">
      {value}
    </p>
  </div>
{/snippet}

<div class="rows">
  {@render row('Display Name', user.displayName ?? null)}
  {@render row('First Name', user.firstName ?? null)}
  {@render row('Middle Name', user.middleName ?? null)}
  {@render row('Last Name', user.lastName ?? null)}
</div>

{#await (async () => {
  if (await server.amIAdmin()) {
    const root = await server.getRootId(user.id)

    return [false, root] as const
  } else {
    return [true, null] as const
  }
})() then [isMine, rootId]}
  {#snippet foreground(view: Snippet)}
    <div class="foreground">
      {@render view()}
    </div>
  {/snippet}
  {#snippet background(view: Snippet)}
    <div class="background">
      {@render view()}
    </div>
  {/snippet}

  {#if (!isMine && rootId != null) || isMine}
    <Button
      {foreground}
      {background}
      onclick={() => {
        goto('/app/files?fileId=' + rootId)
      }}
    >
      {#if isMine}
        Visit My Files
      {:else}
        Visit {user.displayName ?? user.firstName}'s Files
      {/if}
    </Button>
  {:else}
    <p>{user.displayName ?? user.firstName}'s filesystem is not yet initialized.</p>
  {/if}
{/await}

<style lang="scss">
  @use '../../../global.scss' as *;

  div.background {
    background-color: var(--color-1);
    color: var(--color-5);
  }

  div.foreground {
    padding: 8px;
    gap: 8px;
  }

  h2.title {
    font-size: x-large;
  }

  div.rows {
    padding: 8px;
    gap: 8px;
  }

  div.row {
    flex-direction: row;

    gap: 8px;

    > p.label {
      @include force-size(128px, &);

      font-weight: bolder;
      text-align: end;
    }

    > p.label::after {
      content: ': ';
    }

    > p.value {
      flex-grow: 1;
    }
  }
</style>
