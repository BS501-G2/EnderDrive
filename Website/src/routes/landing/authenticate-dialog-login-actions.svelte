<script lang="ts">
  import { useClientContext } from '$lib/client/client'
  import { useLandingContext } from '$lib/client/contexts/landing'
  import Button from '$lib/client/ui/button.svelte'
  import Icon, { type IconOptions } from '$lib/client/ui/icon.svelte'
  import { create } from '$lib/client/utils'
  import { onMount, type Snippet } from 'svelte'

  let googleElement: HTMLDivElement = $state(null as never)
  let jwt: string | null = $state(null as never)
  let googleButtonElement: HTMLButtonElement = $state(null as never)

  const {redirect}:{redirect: () => Promise<void>;}= $props()

  const { server } = useClientContext()

  onMount(() => {
    create(googleElement, async (token) => {
      jwt = token
      googleButtonElement.click()
    })
  })
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
  <div class="google" bind:this={googleElement}></div>
  <div hidden>
    <Button
      bind:buttonElement={googleButtonElement}
      onclick={async () => {
        await server.AuthenticateGoogle({ Token: jwt! })
        await redirect()
      }}>...</Button
    >
  </div>
</div>

<style lang="scss">
  div.actions {
    display: grid;
    gap: 8px;

    // grid-template-columns: repeat(2, calc(50% - 4px));
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
