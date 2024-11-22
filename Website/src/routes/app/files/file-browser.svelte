<script lang="ts">
  import {
    createFileBrowserContext,
    useFileBrowserContext,
    type FileBrowserOptions
  } from '$lib/client/contexts/file-browser'
  import Separator from '$lib/client/ui/separator.svelte'
  import FileBrowserPath from './file-browser-path.svelte'
  import FileManagerActionHost from './file-browser-action-host.svelte'
  import FileBrowserProperties from './file-browser-properties.svelte'
  import { useAppContext } from '$lib/client/contexts/app'
  import FileBrowserResolver from './file-browser-resolver.svelte'
  import FileBrowserAction from './file-browser-action.svelte'
  import { fly } from 'svelte/transition'
  import { onMount } from 'svelte'
  import FileBrowserPropertiesMobile from './file-browser-properties-mobile.svelte'
  import { writable } from 'svelte/store'

  const { resolve, onFileId, selectMode, customContext }: FileBrowserOptions = $props()
  const { actions, top, current, middle, bottom } =
    customContext ?? createFileBrowserContext(onFileId, selectMode)
  const { showDetails, fileListContext } = useFileBrowserContext()
  const { isMobile, isDesktop } = useAppContext()

  const flattenedSelectedIds = writable<string[]>([])

  onMount(() =>
    fileListContext.subscribe((fileListContext) => {
      if (fileListContext != null) {
        fileListContext.selectedFileIds.subscribe((selectedFileIds) => {
          $flattenedSelectedIds = selectedFileIds
        })
      }
    })
  )

  onMount(() =>
    isMobile.subscribe((isMobile) => {
      if (isMobile) {
        $showDetails = false
      }
    })
  )
</script>

<div class="file-browser">
  <div class="left">
    {#if $top.length}
      <div class="top">
        {#each $top as { id, snippet }, index (id)}
          {#if index > 0}
            <Separator horizontal />
          {/if}

          {@render snippet()}
        {/each}
      </div>

      <Separator horizontal />
    {/if}

    <div class="middle">
      {#if resolve}
        <FileBrowserResolver {resolve} {current} {actions} />
      {/if}

      {#each $middle as { id, snippet } (id)}
        {@render snippet()}
      {/each}
    </div>

    {#if $bottom.length}
      <Separator horizontal />

      <div class="bottom">
        {#each $bottom as { id, snippet }, index (id)}
          {#if index > 0}
            <Separator horizontal />
          {/if}

          {@render snippet()}
        {/each}
      </div>
    {/if}
  </div>

  {#if $isDesktop && $showDetails}
    <Separator vertical />

    <div
      class="right"
      transition:fly={{
        x: 16
      }}
    >
      <FileBrowserProperties
        selectedFileIds={$current.type === 'file'
          ? [$current.file.id]
          : $current.type === 'folder'
            ? $flattenedSelectedIds.length
              ? $flattenedSelectedIds
              : [$current.file.id]
            : $flattenedSelectedIds}
      />
    </div>
  {/if}
</div>

{#if $current.type === 'file' || $current.type === 'folder' || $current.type === 'loading'}
  <FileBrowserPath current={$current} />
{/if}

<FileManagerActionHost {actions} />

{#if $current.type !== 'loading' && ($isDesktop || ($isMobile && $flattenedSelectedIds.length))}
  <FileBrowserAction
    label="Details"
    icon={{
      icon: 'info',
      thickness: 'solid'
    }}
    onclick={() => showDetails.update((value) => !value)}
    type="right-main"
  />
{/if}

{#if $isMobile && $showDetails && $fileListContext != null && $flattenedSelectedIds.length > 0}
  <FileBrowserPropertiesMobile selectedFileIds={$flattenedSelectedIds} />
{/if}

<style lang="scss">
  @use '../../../global.scss' as *;

  div.file-browser {
    flex-grow: 1;
    flex-direction: row;

    min-width: 0;
    min-height: 0;

    > div.left {
      flex-grow: 1;
      min-width: 0;
      min-height: 0;

      > div.top {
        flex-direction: column;
      }

      > div.middle {
        flex-grow: 1;
        flex-direction: row;

        overflow: hidden auto;

        min-height: 0;

        > div.loading {
          align-self: center;
        }
      }

      > div.bottom {
        flex-direction: row;
      }
    }

    > div.right {
      @include force-size(320px, &);
    }
  }
</style>
