<script lang="ts">
  import { FileAccessLevel, useServerContext } from '$lib/client/client'
  import type { FileProperties } from '$lib/client/contexts/file-browser'
  import Icon from '$lib/client/ui/icon.svelte'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import Separator from '$lib/client/ui/separator.svelte'
  import { fly } from 'svelte/transition'
  import FileBrowserFileIcon from './file-browser-file-icon.svelte'
  import type { Snippet } from 'svelte'
  import Button from '$lib/client/ui/button.svelte'
  import { createFileBrowserPropertiesContext } from '$lib/client/contexts/file-browser-properties'
  import { useAppContext } from '$lib/client/contexts/app'
  import FileBrowserPropertiesDetailsTab from './file-browser-properties-details-tab.svelte'
  import FileBrowserPropertiesAccessTab from './file-browser-properties-access-tab.svelte'
  import { writable } from 'svelte/store'

  const {
    selectedFileIds
  }: {
    selectedFileIds: string[]
  } = $props()
  const {
    getFile,
    scanFile,
    getMainFileContent,
    getOldestFileSnapshot,
    getLatestFileSnapshot,
    getFileMime,
    getFileSize,
    getFileAccessLevel
  } = useServerContext()

  const promises = writable<Promise<FileProperties[]>>(null as never)
  const { currentTab, tabs } = createFileBrowserPropertiesContext()
  const { isMobile } = useAppContext()

  $effect(() => currentTab.subscribe((tabIndex) => {
    if (!(tabIndex in $tabs)) {
      currentTab.set(0)
    }
  }))

  $effect(() => {
    $promises = (() =>
      Promise.all(
        selectedFileIds.map(async (fileId): Promise<FileProperties> => {
          const file = await getFile(fileId)
          const fileAccessLevel = await getFileAccessLevel(file.id)
          const fileContent = await getMainFileContent(file.id)
          const fileOldestSnapshot = await getOldestFileSnapshot(file.id, fileContent.id)
          const fileLatestSnapshot = await getLatestFileSnapshot(file.id, fileContent.id)

          const viruses =
            fileLatestSnapshot != null
              ? await scanFile(file.id, fileContent.id, fileLatestSnapshot.id)
              : null
          const mime = await getFileMime(file.id)
          const size = await getFileSize(file.id, fileContent.id, fileLatestSnapshot?.id)

          const modified =
            fileLatestSnapshot?.createTime != null
              ? new Date(fileLatestSnapshot.createTime)
              : new Date()
          const created =
            fileOldestSnapshot?.createTime != null
              ? new Date(fileOldestSnapshot.createTime)
              : new Date()

              return {
            file,
            fileAccessLevel,
            viruses,

            created,
            modified,
            mime,
            size
          }
        })
      ))()
  })
</script>

<div
  class="properties"
  transition:fly={{
    x: 16,
    duration: 150
  }}
>
  {#if $promises != null}
    {#await $promises}
      <div class="loading" in:fly|global={{ x: 16 }}>
        <LoadingSpinner size="3rem" />
      </div>
    {:then files}
      {#if files.length > 0}
        <div class="header">
          <div class="preview">
            {#if files.length === 1}
              <FileBrowserFileIcon mime={files[0]?.mime} size="72px" />
            {:else if files.length === 0}
              <Icon icon="file" size="72px" />
            {:else}
              <Icon icon="file" size="72px" />
            {/if}
            <!-- <FileMime -->
          </div>

          <p class="title">
            {#if files.length > 1}
              {files.length} files
            {:else}
              {files[0]?.file.name}
            {/if}
          </p>
        </div>

        <Separator horizontal />

        <div class="details" in:fly|global={{ x: 16 }}>
          <div class="tabs">
            {#each $tabs as { id, name, icon }, index (id)}
              {#snippet foreground(view: Snippet)}
                <div class="tab-button">
                  {@render view()}
                </div>
              {/snippet}

              <div class="entry">
                <Button {foreground} onclick={() => currentTab.set(index)}>
                  <Icon {...icon} />
                  <p class="label">
                    {name}
                  </p>
                </Button>

                {#if $currentTab === index}
                  <div class="indicator" class:mobile={$isMobile}></div>
                {/if}
              </div>
            {/each}
          </div>

          <Separator horizontal />

          <div class="tab-content">
            {@render $tabs[$currentTab]?.content()}
          </div>
        </div>
      {:else}
        <div class="empty">
          <Icon icon="file" size="72px" />
          <p>Select any file to examine</p>
        </div>
      {/if}

      {#if files.length > 0}
        <FileBrowserPropertiesDetailsTab {files} />
      {/if}

      {#if files.length === 1 && files[0].file.parentId != null && files[0].fileAccessLevel >= FileAccessLevel.Manage}
        <FileBrowserPropertiesAccessTab file={files[0]} />
      {/if}

      {#if files.length === 1}{/if}
    {/await}
  {/if}
</div>

<style lang="scss">
  @use '../../../global.scss' as *;

  div.loading {
    flex-grow: 1;

    flex-direction: row;
    align-items: center;
    justify-content: center;
  }

  div.properties {
    flex-grow: 1;

    overflow: hidden auto;
  }

  div.header {
    gap: 32px;
    padding: 32px;

    > div.preview {
      align-items: center;
      justify-content: center;
    }

    > p.title {
      overflow: hidden;

      font-size: 1.2em;
      font-weight: bolder;
      text-align: center;
      word-wrap: break-word;
    }
  }

  div.details {
    flex-grow: 1;

    > div.tabs {
      flex-direction: row;
      overflow: auto hidden;

      div.entry {
        flex-grow: 1;

        div.tab-button {
          flex-direction: row;
          align-items: center;

          padding: 8px;
          gap: 8px;
        }

        div.indicator {
          @include force-size(&, 1px);

          background-color: var(--color-1);
        }

        div.indicator.mobile {
          background-color: var(--color-5);
        }
      }
    }

    > div.tab-content {
      flex-grow: 1;
    }
  }

  div.empty {
    gap: 16px;
    flex-grow: 1;

    align-items: center;
    justify-content: center;
  }
</style>
