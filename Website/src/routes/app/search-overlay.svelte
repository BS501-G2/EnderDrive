<script
  lang="ts"
>
  import type { Snippet } from 'svelte';
  import SearchUsers from './search-users.svelte';
  import {
    useClientContext,
    useServerContext
  } from '$lib/client/client';
  import Button from '$lib/client/ui/button.svelte';
  import Input from '$lib/client/ui/input.svelte';
  import { useAppContext } from '$lib/client/contexts/app';
  import SearchFiles from './search-files.svelte';

  const {
    isMobile
  } =
    useAppContext();
  const {
    windowButtons,
    ondismiss
  }: {
    windowButtons: Snippet;
    ondismiss: () => void;
  } =
    $props();

  let searchString: string =
    $state(
      ''
    );
</script>

{#snippet resultBox(
  name: string,
  seeMore: () => void,
  snippet: Snippet
)}
  <div
    class="result-box"
  >
    <div
      class="header"
    >
      <h2
      >
        {name}
      </h2>

      {#snippet buttonBackground(
        view: Snippet
      )}
        <div
          class="button-background"
        >
          {@render view()}
        </div>
      {/snippet}

      {#snippet buttonForeground(
        view: Snippet
      )}
        <div
          class="button-foreground"
        >
          {@render view()}
        </div>
      {/snippet}

      <Button
        onclick={() =>
          seeMore()}
        background={buttonBackground}
        foreground={buttonForeground}
      >
        <!-- <Icon icon="ellipsis" thickness="solid" /> -->

        See
        More
      </Button>
    </div>

    <div
      class="separator"
    ></div>

    <div
      class="body"
    >
      {@render snippet()}
    </div>
  </div>
{/snippet}

<div
  class="search"
  class:mobile={$isMobile}
>
  <div
    class="header"
  >
    <p
      class="title"
    >
      Search
    </p>
    {@render windowButtons()}
  </div>

  <div
    class="body"
  >
    <div
      class="search-field"
    >
      <Input
        id="search"
        type="text"
        name="Search String"
        bind:value={searchString}
      />
    </div>

    {#if searchString.length}
      <div
        class="result"
      >
        <SearchUsers
          {searchString}
          card={resultBox}
          {ondismiss}
        />
        <SearchFiles
          {searchString}
          card={resultBox}
          {ondismiss}
        />
      </div>
    {:else}
      <div
        class="placeholder"
      >
        <h2
        >
          Search
        </h2>
        <p
        >
          You
          can
          use
          the
          search
          feature
          to
          find
          users
          and
          files.
        </p>
      </div>
    {/if}
  </div>
</div>

<style
  lang="scss"
>
  @use '../../global.scss'
    as *;

  div.search {
    background-color: var(
      --color-9
    );

    // box-shadow: 2px 2px 4px;
    gap: 8px;

    text-align: start;

    min-height: 0;

    @include force-size(
      min(
        50dvw,
        480px
      ),
      min(
        50dvh,
        720px
      )
    );

    > div.header {
      flex-direction: row;
      align-items: center;

      > p.title {
        flex-grow: 1;

        font-size: 1.2em;
        font-weight: bold;

        margin: 0
          8px;
      }
    }

    > div.body {
      flex-grow: 1;

      min-height: 0;

      padding: 8px;

      > div.search-field {
        background-color: var(
          --color-9
        );
      }

      // > div.user-tab {

      > div.placeholder {
        flex-grow: 1;
        gap: 8px;

        align-items: center;
        justify-content: center;

        > h2 {
          font-size: 1.2rem;
        }
      }

      > div.result {
        flex-grow: 1;

        overflow: hidden
          auto;
        min-height: 0;
      }
    }
  }

  div.result-box {
    // background-color: var(--color-9);

    > div.header {
      flex-direction: row;
      align-items: center;

      padding: 8px;

      > h2 {
        flex-grow: 1;
        font-size: 1.5em;

        padding: 0
          8px;
      }

      div.button-background {
        background-color: var(
          --color-1
        );
        color: var(
          --color-5
        );
      }

      div.button-foreground {
        padding: 8px;
      }
    }

    > div.separator {
      background-color: var(
        --color-5
      );
      margin: 0
        8px;

      @include force-size(
        &,
        1px
      );
    }

    > div.body {
      flex-grow: 1;
      align-items: stretch;

      min-height: 128px;

      padding: 8px;

      overflow: auto
        hidden;
    }
  }

  div.search.mobile {
    @include force-size(
      100dvw,
      100dvh
    );

    box-sizing: border-box;
  }
</style>
