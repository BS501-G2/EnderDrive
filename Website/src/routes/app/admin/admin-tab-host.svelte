<script lang="ts">
  import { goto } from '$app/navigation'
  import { page } from '$app/stores'
  import type { Tab } from '$lib/client/contexts/admin'
  import Button from '$lib/client/ui/button.svelte'
  import { type Snippet } from 'svelte'
  import type { Readable } from 'svelte/store'
  import Icon from '$lib/client/ui/icon.svelte'
  import Separator from '$lib/client/ui/separator.svelte'
  import { useAppContext } from '$lib/client/contexts/app'

  const {
    tabs
  }: {
    tabs: Readable<Tab[]>
  } = $props()
  const { isMobile } = useAppContext()
</script>

<div class="tabs">
  <div class="background" class:mobile={$isMobile}>
    {#key $page}
      {#each $tabs as { id, name, callback }, index (id)}
        <!-- {#if index !== 0}
					<div class="separator"></div>
				{/if} -->

        {@const { path, icon, selected } = callback($page)}

        <button
          class="tab"
          class:mobile={$isMobile}
          class:selected
          onclick={() => {
            goto(path)
          }}
        >
          <div class="label">
            <Icon {...icon} />
            <p>
              {name}
            </p>
          </div>

          {#if selected}
            <div class="indicator"></div>
          {/if}
        </button>
      {/each}
    {/key}
  </div>
</div>

<style lang="scss">
  @use '../../../global.scss' as *;

  div.tabs {
    flex-direction: row;

    overflow: auto hidden;
  }

  div.background {
    flex-direction: row;
    background-color: var(--color-5);
    color: var(--color-1);
  }

  div.background.mobile {
    flex-grow: 1;

    background-color: unset;
    color: unset;
  }

  div.separator {
    @include force-size(1px, &);

    background-color: var(--color-1);
    margin: 4px 0;
  }

  button.tab {
    display: flex;
    flex-direction: column;
    align-items: stretch;

    border: none;
    border-radius: 8px 8px 0 0;

    cursor: pointer;

    > div.label {
      flex-direction: row;

      align-items: center;

      padding: 8px 16px;
      gap: 8px;
    }
  }

  button.tab.mobile {
    flex-grow: 1;
    flex-basis: 0;

    > div.label {
      flex-grow: 1;
      gap: 0;
      flex-direction: column;

      justify-content: flex-end;
    }

    border-radius: 0;
  }

  button.tab.selected {
    background-color: var(--color-1);
    color: var(--color-5);
  }

  button.tab.selected.mobile {
    background-color: unset;
    color: unset;

    > div.label {
      > p {
        font-weight: bolder;
      }
    }

    > div.indicator {
      @include force-size(&, 1px);

      background-color: var(--color-1);

      align-self: stretch;
    }
  }
</style>
