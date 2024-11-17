<script
  lang="ts"
>
  import {
    useFileBrowserContext,
    type FileBrowserAction
  } from '$lib/client/contexts/file-browser';
  import Separator from '$lib/client/ui/separator.svelte';
  import {
    onMount,
    type Snippet
  } from 'svelte';
  import type { Readable } from 'svelte/store';
  import { derived } from 'svelte/store';

  const {
    actions
  }: {
    actions: Readable<
      FileBrowserAction[]
    >;
  } =
    $props();
  const {
    pushTop
  } =
    useFileBrowserContext();

  onMount(
    () =>
      pushTop(
        desktopContent
      )
  );

  const leftMain =
    derived(
      actions,
      (
        actions
      ) =>
        actions.filter(
          (
            entry
          ) =>
            entry.type ===
            'left-main'
        )
    );
  const left =
    derived(
      actions,
      (
        actions
      ) =>
        actions.filter(
          (
            entry
          ) =>
            entry.type ===
            'left'
        )
    );
  const rightMain =
    derived(
      actions,
      (
        actions
      ) =>
        actions.filter(
          (
            entry
          ) =>
            entry.type ===
            'right-main'
        )
    );
  const right =
    derived(
      actions,
      (
        actions
      ) =>
        actions.filter(
          (
            entry
          ) =>
            entry.type ===
            'right'
        )
    );
</script>

{#snippet desktopContent()}
  <div
    class="actions-container"
  >
    {#if $leftMain.length}
      <div
        class="actions left main"
      >
        {#each $leftMain as { id, snippet } (id)}
          {@render snippet()}
        {/each}
      </div>

      <Separator
        vertical
      />
    {/if}

    <div
      class="actions left not-main"
    >
      {#each $left as { id, snippet } (id)}
        {@render snippet()}
      {/each}
    </div>

    <div
      class="actions right not-main"
    >
      {#each $right as { id, snippet } (id)}
        {@render snippet()}
      {/each}
    </div>

    {#if $rightMain.length}
      <Separator
        vertical
      />

      <div
        class="actions right main"
      >
        {#each $rightMain as { id, snippet } (id)}
          {@render snippet()}
        {/each}
      </div>
    {/if}
  </div>
{/snippet}

<style
  lang="scss"
>
  @use '../../../global.scss'
    as *;

  div.actions-container {
    flex-direction: row;

    overflow: auto
      hidden;

    min-width: 0;
  }

  div.actions {
    flex-direction: row;
    align-items: center;

    padding: 8px;
  }

  div.actions.left.not-main,
  div.actions.right.not-main {
    flex-grow: 1;
  }

  div.actions.right {
    justify-content: flex-end;
  }
</style>
