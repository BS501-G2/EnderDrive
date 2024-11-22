<script lang="ts">
  import Button from '$lib/client/ui/button.svelte'
  import Icon from '$lib/client/ui/icon.svelte'
  import Separator from '$lib/client/ui/separator.svelte'
  import type { Snippet } from 'svelte'
  import { writable } from 'svelte/store'
  import { fly } from 'svelte/transition'

  const { title, children }: { title: string; children?: Snippet } = $props()

  const display = writable<boolean>(false)
</script>

<div class="expando">
  <button
    class="header"
    onclick={() => {
      $display = !$display
    }}
  >
    <div class="title">
      <h2 class="title">{title}</h2>
      {#key $display}
        <Icon icon="chevron-{$display ? 'up' : 'down'}" size="1.2em" thickness="solid"></Icon>
      {/key}
    </div>
  </button>
  <Separator horizontal />

  {#if $display}
    <div class="body" transition:fly={{ y: -16 }}>
      {@render children?.()}
    </div>
  {/if}
</div>

<style lang="scss">
  button.header {
    border: none;
    cursor: pointer;

    > div.title {
      flex-direction: row;
      color: var(--color-2);
      align-items: center;
      text-align: start;

      > h2.title {
        flex-grow: 1;

        font-size: 2em;
      }
    }
  }

  div.body {
    padding: 8px;
    gap: 8px;
  }
</style>
