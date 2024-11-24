<script lang="ts">
  import type { Writable } from 'svelte/store'
  import type { UserContext } from './user-context'
  import Icon, { type IconOptions } from '$lib/client/ui/icon.svelte'
  import type { Snippet } from 'svelte'
  import Button from '$lib/client/ui/button.svelte'
  import Separator from '$lib/client/ui/separator.svelte'

  const {
    tabs,
    currentTabIndex
  }: {
    tabs: Writable<{ id: number; label: string; icon: IconOptions }[]>
    currentTabIndex: Writable<number>
  } = $props()
</script>

<div class="tabs">
  {#each $tabs as { id, label, icon }, index (id)}
    {#snippet foreground(view: Snippet)}
      <div class="foreground">
        {@render view()}
      </div>
    {/snippet}

    <div class="tab">
      <Button
        {foreground}
        onclick={() => {
          currentTabIndex.set(index)
        }}
      >
        <Icon {...icon} />

        <p>{label}</p>
      </Button>

      {#if index == $currentTabIndex}
        <div class="separator"></div>
      {/if}
    </div>
  {/each}
</div>

<style lang="scss">
  @use '../../../global.scss' as *;

  div.tabs {
    flex-direction: row;

    padding: 0 72px;
  }

  div.tab {
    div.separator {
      @include force-size(&, 1px);

      background-color: var(--color-1);
    }

    div.foreground {
      flex-grow: 1;

      flex-direction: row;

      gap: 8px;
      padding: 8px;
    }
  }
</style>
