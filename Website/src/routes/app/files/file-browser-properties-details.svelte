<script
  lang="ts"
>
  import type { FileProperties } from '$lib/client/contexts/file-browser';
  import Button from '$lib/client/ui/button.svelte';
  import Icon, {
    type IconOptions
  } from '$lib/client/ui/icon.svelte';
  import Separator from '$lib/client/ui/separator.svelte';
  import {
    onMount,
    type Snippet
  } from 'svelte';
  import {
    writable,
    type Writable
  } from 'svelte/store';
  import { useAppContext } from '$lib/client/contexts/app';

  const {
    files
  }: {
    files: FileProperties[];
  } =
    $props();

  const {
    isMobile
  } =
    useAppContext();
  const tabs: Writable<
    {
      id: number;
      name: string;
      icon: IconOptions;
      content: Snippet;
    }[]
  > =
    writable(
      []
    );
  const currentTab: Writable<number> =
    writable(
      0
    );

  function pushTab(
    name: string,
    icon: IconOptions,
    content: Snippet
  ) {
    const id =
      Math.random();

    tabs.update(
      (
        value
      ) => [
        ...value,
        {
          id,
          name,
          icon,
          content
        }
      ]
    );

    return () =>
      tabs.update(
        (
          value
        ) =>
          value.filter(
            (
              tab
            ) =>
              tab.id !==
              id
          )
      );
  }

  onMount(
    () =>
      pushTab(
        'Overview',
        {
          icon: 'info',
          thickness:
            'solid'
        },
        overview
      )
  );
</script>

{#snippet overview()}{/snippet}

<div
  class="details"
>
  <div
    class="tabs"
  >
    {#each $tabs as { id, name, icon }, index (id)}
      {#snippet foreground(
        view: Snippet
      )}
        <div
          class="tab-button"
        >
          {@render view()}
        </div>
      {/snippet}

      <div
        class="entry"
      >
        <Button
          {foreground}
          onclick={() =>
            currentTab.set(
              index
            )}
        >
          <Icon
            {...icon}
          />
          <p
            class="label"
          >
            {name}
          </p>
        </Button>

        {#if $currentTab === index}
          <div
            class="indicator"
            class:mobile={$isMobile}
          ></div>
        {/if}
      </div>
    {/each}
  </div>

  <div
    class="tab-content"
  >
    {@render $tabs[
      $currentTab
    ]?.content()}
  </div>
</div>

<style
  lang="scss"
>
  @use '../../../global.scss'
    as *;

  div.tabs {
    flex-direction: row;
    overflow: auto
      hidden;

    div.entry {
      flex-grow: 1;

      div.tab-button {
        flex-direction: row;
        align-items: center;

        padding: 8px;
        gap: 8px;

        p.label {
        }
      }

      div.indicator {
        @include force-size(
          &,
          1px
        );

        background-color: var(
          --color-1
        );
      }
      div.indicator.mobile {
        background-color: var(
          --color-5
        );
      }
    }
  }
</style>
