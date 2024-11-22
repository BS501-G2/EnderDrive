<script lang="ts">
  import Button from '$lib/client/ui/button.svelte'
  import Icon, { type IconOptions } from '$lib/client/ui/icon.svelte'
  import { type Snippet } from 'svelte'
</script>

{#snippet action(name: string, icon: IconOptions, onclick: () => void)}
  {#snippet actionBackground(view: Snippet)}
    <div class="action-container-outer">
      {@render view()}
    </div>
  {/snippet}

  {#snippet actionForeground(view: Snippet)}
    <div class="action-container">
      {@render view()}
    </div>
  {/snippet}

  <Button background={actionBackground} foreground={actionForeground} {onclick}>
    <div class="action">
      <Icon {...icon} />
      <p>
        {name}
      </p>
    </div>
  </Button>
{/snippet}

<div class="actions">
  {@render action(
    'Google',
    {
      brand: true,
      icon: 'google'
    },
    async () => {}
  )}

  {@render action(
    'Reset Password',
    {
      thickness: 'solid',
      icon: 'key'
    },
    async () => {
      // const
    }
  )}
</div>

<style lang="scss">
  div.actions {
    display: grid;
    gap: 8px;

    grid-template-columns: repeat(2, calc(50% - 4px));
  }

  div.action-container-outer {
    background-color: transparent;
    color: var(--color-1);
    border: solid 1px var(--color-1);

    flex-grow: 1;
  }

  div.action-container {
    padding: 8px;
  }

  div.action {
    flex-grow: 1;
    flex-direction: row;
    align-items: center;

    gap: 8px;

    > p {
      flex-grow: 1;
    }
  }
</style>
