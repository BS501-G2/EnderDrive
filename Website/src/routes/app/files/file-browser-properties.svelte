<script lang="ts">
  import { useClientContext } from '$lib/client/client'
  import { useFileBrowserContext, type FileProperties } from '$lib/client/contexts/file-browser'
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
  import FileBrowserPropertiesTranscriptTab from './file-browser-properties-transcript-tab.svelte'
  import FileBrowserPropertiesLogsTab from './file-browser-properties-logs-tab.svelte'
  import FileBrowserPropertiesSnapshots from './file-browser-properties-snapshots.svelte'
  import { FileAccessLevel, FileType } from '$lib/client/resource'

  const {
    selectedFileIds
  }: {
    selectedFileIds: string[]
  } = $props()
  const { server } = useClientContext()

  const { current } = useFileBrowserContext()
  const promises = writable<Promise<FileProperties[]>>(null as never)
  const { currentTab, tabs } = createFileBrowserPropertiesContext()
  const { isMobile } = useAppContext()

  $effect(() =>
    currentTab.subscribe((tabIndex) => {
      if (!(tabIndex in $tabs)) {
        currentTab.set(0)
      }
    })
  )

  $effect(() => {
    $promises = (() =>
      Promise.all(
        selectedFileIds.map(async (fileId): Promise<FileProperties> => {
          const file = await server.GetFile({ FileId: fileId })
          const fileAccessLevel = await server.GetFileAccessLevel({ FileId: fileId })

          if (file.Type == FileType.File) {
            const fileDataEntries = await server.FileGetDataEntries({ FileId: file.Id })
            const oldData = fileDataEntries.at(-1)!
            const newData = fileDataEntries[0]

            const viruses = await server.FileScan({
              FileId: file.Id,
              FileDataId: newData.Id
            })
            const mime = await server.FileGetMime({ FileId: file.Id, FileDataId: newData.Id })
            const size = await server.FileGetSize({
              FileId: file.Id,
              FileDataId: newData.Id
            })

            const modified = new Date(newData.CreateTime)
            const created = new Date(oldData.CreateTime)

            return {
              type: 'file',

              file,
              fileAccessLevel,
              viruses,

              created,
              modified,
              mime,
              size
            }
          } else if (file.Type == FileType.Folder) {
            const fileLogs = await server.GetFileLogs({ FileId: file.Id, UniqueFileId: false })
            const oldLog = fileLogs.at(-1)!
            const newLog = fileLogs[0]

            const count = await server
              .GetFiles({ ParentFolderId: file.Id })
              .then((result) => result.length)

            const modified = new Date(newLog.CreateTime)
            const created = new Date(oldLog.CreateTime)

            return {
              type: 'folder',

              count,

              file,
              modified,
              created,
              fileAccessLevel
            }
          } else {
            throw new Error('Invalid file type: ' + file.Type)
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
            {#if files.length === 1 && files[0].type === 'file'}
              <FileBrowserFileIcon mime={files[0].mime} size="72px" />
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
              {files[0]?.file.Name}
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

      {#if files.length === 1 && files[0].file.ParentId != null}
        {#if files[0].fileAccessLevel >= FileAccessLevel.Manage}
          <FileBrowserPropertiesAccessTab file={files[0]} />
        {/if}

        {#if files[0].file.Type === FileType.File && $current.type === 'file'}
          <FileBrowserPropertiesSnapshots file={files[0]} />
        {/if}

        {#if files[0].type === 'file' && (files[0].mime.startsWith('audio/') || files[0].mime.startsWith('video/'))}
          <FileBrowserPropertiesTranscriptTab file={files[0]} />
        {/if}

        <FileBrowserPropertiesLogsTab file={files[0]} />
      {/if}
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

    min-height: 0;

    > div.tabs {
      flex-direction: row;
      overflow: auto hidden;

      flex-shrink: 0;

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

        p.label {
          text-wrap: nowrap;
        }
      }
    }

    > div.tab-content {
      flex-grow: 1;

      min-height: 0;
    }
  }

  div.empty {
    gap: 16px;
    flex-grow: 1;

    align-items: center;
    justify-content: center;
  }
</style>
