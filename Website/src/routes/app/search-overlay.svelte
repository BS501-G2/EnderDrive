<script lang="ts">
  import type { Snippet } from 'svelte'
  import SearchUsers from './search-users.svelte'
  import Button from '$lib/client/ui/button.svelte'
  import Input from '$lib/client/ui/input.svelte'
  import { useAppContext } from '$lib/client/contexts/app'
  import SearchFiles from './search-files.svelte'
  import { writable } from 'svelte/store'
  import { fly } from 'svelte/transition'
  import Overlay from '../overlay.svelte'

  const { isMobile } = useAppContext()
  const {
    ondismiss,
    searchButton
  }: {
    searchButton?: HTMLButtonElement
    ondismiss: () => void
  } = $props()

  const bounds = $derived(searchButton?.getBoundingClientRect())

  const searchString = writable('')
</script>

{#snippet resultBox(name: string, seeMore: () => void, snippet: Snippet)}
  <div class="result-box">
    <div class="header">
      <h2>
        {name}
      </h2>

      {#snippet buttonBackground(view: Snippet)}
        <div class="button-background">
          {@render view()}
        </div>
      {/snippet}

      {#snippet buttonForeground(view: Snippet)}
        <div class="button-foreground">
          {@render view()}
        </div>
      {/snippet}

      <Button onclick={() => seeMore()} background={buttonBackground} foreground={buttonForeground}>
        <!-- <Icon icon="ellipsis" thickness="solid" /> -->

        See More
      </Button>
    </div>

    <div class="separator"></div>

    <div class="body">
      {@render snippet()}
    </div>
  </div>
{/snippet}

<Overlay {ondismiss} y={0} x={bounds != null ? bounds.x - 8 : undefined} notransition>
  {#snippet children()}
    <div
      class="search"
      class:mobile={$isMobile}
      style:min-width={bounds?.width != null ? `${bounds.width + 16}px` : '100dvw'}
      style:max-width={bounds?.width != null ? `${bounds.width + 16}px` : '100dvw'}
    >
      <div class="body">
        <div class="search-field">
          <Input
            id="search"
            icon={{ icon: 'magnifying-glass', thickness: 'solid', size: '1em' }}
            type="text"
            bind:value={$searchString}
            placeholder="Search..."
          />
        </div>

        {#if $searchString.length}
          <div class="result">
            <SearchUsers searchString={$searchString} card={resultBox} {ondismiss} />
            <SearchFiles searchString={$searchString} card={resultBox} {ondismiss} />
          </div>
        {:else}
          <div class="placeholder">
            <p>You can use the search feature to find users and files.</p>
          </div>
        {/if}
      </div>
    </div>
  {/snippet}
</Overlay>

<style lang="scss">
  @use '../../global.scss' as *;

  div.search {
    background-color: var(--color-9);

    // box-shadow: 2px 2px 4px;
    gap: 8px;

    text-align: start;

    min-height: 0;

    // @include force-size(&, min(50dvh, 720px));

    > div.header {
      flex-direction: row;
      align-items: center;

      > p.title {
        flex-grow: 1;

        font-size: 1.2em;
        font-weight: bold;

        margin: 0 8px;
      }
    }

    > div.body {
      flex-grow: 1;

      min-height: 0;

      padding: 8px;
      gap: 8px;

      > div.search-field {
        background-color: var(--color-9);

        flex-direction: row;

        min-height: 34px;
      }

      // > div.user-tab {

      > div.placeholder {
        flex-grow: 1;
        gap: 8px;
      }

      > div.result {
        flex-grow: 1;

        overflow: hidden auto;
        min-height: 0;
      }
    }
  }

  div.result-box {
    // background-color: var(--color-9);

    > div.header {
      flex-direction: row;
      align-items: center;

      > h2 {
        flex-grow: 1;
        font-size: 1.5em;

        padding: 0 8px;
      }

      div.button-background {
        background-color: transparent;
        color: inherit;
      }

      div.button-foreground {
        padding: 8px;
      }
    }

    > div.separator {
      background-color: var(--color-5);
      margin: 0 8px;

      @include force-size(&, 1px);
    }

    > div.body {
      flex-grow: 1;
      align-items: stretch;

      min-height: 128px;

      padding: 0px;

      overflow: auto hidden;
    }
  }

  div.search.mobile {
    @include force-size(100dvw, 100dvh);

    box-sizing: border-box;
  }
</style>
