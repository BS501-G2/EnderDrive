<script lang="ts">
  import type {
    createFileBrowserContext,
    FileBrowserOptions,
    FileBrowserResolve,
    FileBrowserResolveType
  } from '$lib/client/contexts/file-browser'
  import Button from '$lib/client/ui/button.svelte'
  import type { Readable, Writable } from 'svelte/store'
  import FileBrowser from './file-browser.svelte'
  import { type Snippet } from 'svelte'

  const {
    selectMode,
    resolve,
    selectedFiles,
    fileBrowserContext,
    oncancel,
    onfiles,
    maxFileCount
  }: {
    maxFileCount: number
    resolve: Readable<
      Exclude<FileBrowserResolve, [FileBrowserResolveType.Trash]>
    >
    selectMode: FileBrowserOptions['selectMode']
    selectedFiles: Writable<string[]>
    fileBrowserContext: ReturnType<typeof createFileBrowserContext>
    oncancel: () => void
    onfiles: (
      event: MouseEvent & {
        currentTarget: EventTarget & HTMLButtonElement
      },
      files: string[]
    ) => Promise<void>
  } = $props()

  $effect(() =>
    selectedFiles.subscribe((files) => {
      if (files.length > maxFileCount) {
        setTimeout(() => {
          try {
            selectedFiles.set(files.slice(0, length))
          } catch (error) {}
        }, 1)
      }
    })
  )
</script>

<div class="file-selector">
  <div class="area">
    <div class="side"></div>
    <div class="main">
      <FileBrowser
        {resolve}
        {selectMode}
        onFileId={async (event, fileId) => {
          console.log(fileId)
          if (fileId == null) {
            return
          }

          onfiles(event, [fileId])
        }}
        customContext={fileBrowserContext}
      ></FileBrowser>
    </div>
  </div>
  <div class="actions">
    {#snippet foreground(view: Snippet)}
      <div class="foreground">
        {@render view()}
      </div>
    {/snippet}

    <Button
      onclick={(event) => {
        onfiles(event, $selectedFiles.slice(0, maxFileCount))
      }}
      {foreground}
      disabled={$selectedFiles.length === 0 ||
        $selectedFiles.length > maxFileCount}
    >
      <p>Select</p>
    </Button>

    <Button
      onclick={() => {
        oncancel()
      }}
      {foreground}
    >
      <p>Cancel</p>
    </Button>
  </div>
</div>

<style lang="scss">
  div.file-selector {
    flex-grow: 1;
    min-height: 0;
    // background-color: var(--color-5);
    // color: var(--color-1);

    > div.area {
      flex-grow: 1;
      min-height: 0;

      > div.main {
        flex-grow: 1;
        min-height: 0;
      }
    }

    > div.actions {
      flex-direction: row;

      justify-content: flex-end;

      div.foreground {
        padding: 8px;
      }
    }
  }
</style>
