<script
  lang="ts"
>
  import { useDashboardContext } from '$lib/client/contexts/dashboard';
  import Icon from '$lib/client/ui/icon.svelte';
  import Overlay from '../overlay.svelte';
  import {
    onMount,
    type Snippet
  } from 'svelte';
  import SearchOverlay from './search-overlay.svelte';
  import Button from '$lib/client/ui/button.svelte';
  import AppButton from './app-button.svelte';
  import { loremIpsum } from 'lorem-ipsum';

  const {
    pushDesktopTopMiddle
  } =
    useDashboardContext();

  let searchOverlay: boolean =
    $state(
      false
    );

  onMount(
    () =>
      pushDesktopTopMiddle(
        desktopContent
      )
  );
</script>

<AppButton
  label="Search"
  onclick={() =>
    (searchOverlay = true)}
  icon={{
    icon: 'magnifying-glass',
    thickness:
      'solid'
  }}
  show
/>

{#snippet desktopContent()}
  <button
    class="search"
    aria-label="Search"
    onclick={() => {
      searchOverlay = true;
    }}
  >
    <div
      class="icon"
    >
      <Icon
        icon="magnifying-glass"
        thickness="solid"
        size="1em"
      />
    </div>
    <div
      class="space"
    >
      Search...
    </div>
  </button>
{/snippet}

{#if searchOverlay}
  <Overlay
    ondismiss={() => {
      searchOverlay = false;
    }}
  >
    {#snippet children(
      windowButtons
    )}
      <SearchOverlay
        {windowButtons}
        ondismiss={() => {
          searchOverlay = false;
        }}
      />
    {/snippet}
  </Overlay>
{/if}

<style
  lang="scss"
>
  @use '../../global.scss'
    as *;

  button.search {
    display: flex;
    flex-direction: row;
    align-items: center;

    -webkit-app-region: no-drag;

    gap: 8px;
    padding: 4px
      8px;
    box-sizing: border-box;

    border: none;
    outline: solid
      1px
      var(
        --color-1
      );

    background-color: var(
      --color-9
    );

    transition-duration: 150ms;
    // transition-property: box-shadow;

    @include force-size(
      max(
        25%,
        320px
      ),
      32px
    );

    cursor: text;

    > div.icon {
      justify-content: center;
      line-height: 1em;
    }

    > div.space {
      flex-grow: 1;

      color: var(
        --color-1
      );
    }
  }

  button.search:hover {
    box-shadow: 1px
      1px
      4px;
  }
</style>
