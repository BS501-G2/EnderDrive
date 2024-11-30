<script lang="ts">
  import { useClientContext } from '$lib/client/client';
  import Button from '$lib/client/ui/button.svelte'
  import Icon, { type IconOptions } from '$lib/client/ui/icon.svelte'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import { type Snippet } from 'svelte'

  const { redirect, ondismiss }: { redirect: () => Promise<void>, ondismiss: () => void } = $props()
    const { server } = useClientContext()
</script>

{#await server.Me({})}
  <div class="loading">
    <LoadingSpinner size="3em" />
  </div>
{:then me}
  <div class="existing-login">
    {#snippet button(
      name: string,
      icon: IconOptions,
      isPrimary: boolean,
      onclick: () => Promise<void>
    )}
      {#snippet background(view: Snippet)}
        <div class="background">
          {@render view()}
        </div>
      {/snippet}
      {#snippet foreground(view: Snippet)}
        <div class="foreground" class:primary={isPrimary}>
          {@render view()}
        </div>
      {/snippet}

      <Button {background} {foreground} {onclick}>
        <Icon {...icon} />
        <p class="name">
          {name}
        </p>
      </Button>
    {/snippet}

    {@render button('Continue as ' + (me.DisplayName ?? me.FirstName), { icon: 'user' }, true, async () => {
      ondismiss()
      await redirect()
    })}

    {@render button('Log Out', { icon: 'sign-out', thickness: 'solid' }, false, async () => {
      await server.Deauthenticate({})
    })}
  </div>
{/await}

<style lang="scss">
  div.loading {
    align-items: center;
  }

  div.existing-login {
    gap: 8px;

    div.background {
      flex-grow: 1;
    }

    div.foreground {
      flex-grow: 1;

      flex-direction: row;
      align-items: center;
      justify-content: center;

      gap: 8px;
      padding: 8px;

      border: solid 1px var(--color-1);

      p.name {
        flex-grow: 1;
      }
    }

    div.foreground.primary {
      background-color: var(--color-1);
      color: var(--color-5);

      text-align: center;
    }
  }
</style>
