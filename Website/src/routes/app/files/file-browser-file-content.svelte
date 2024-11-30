<script lang="ts">
  import { useClientContext } from '$lib/client/client'
  import { useAppContext } from '$lib/client/contexts/app'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import { derived, writable, type Writable } from 'svelte/store'
  import FileBrowserActions from './file-browser-actions.svelte'
  import FileBrowserFileContentView from './file-browser-file-content-view.svelte'
  import { type Snippet } from 'svelte'
  import { page } from '$app/stores'

  const {
    fileId,
    selectedFileIds
  }: {
    fileId: string
    selectedFileIds: Writable<string[]>
  } = $props()
  const { server } = useClientContext()
  const { isDesktop } = useAppContext()
  const fileDataId = derived(page, ({ url }) => url.searchParams.get('dataId') || undefined)

  async function load(customDataId?: string) {
    const file = await server.GetFile({ FileId: fileId })
    const me = await server.Me({})
    const fileData = await server
      .FileGetDataEntries({
        FileId: file.Id,
        FileDataId: customDataId,
        Pagination: { Count: 1 }
      })
      .then((result) => result[0])

    const mime = await server.FileGetMime({ FileId: fileId, FileDataId: fileData.Id })
    const virus = await server.FileScan({ FileId: file.Id, FileDataId: fileData.Id })

    return {
      file,
      mime,
      fileData,
      virus,
      me
    }
  }

  let promise = writable(load($fileDataId || undefined))
</script>

{#snippet loading()}
  <div class="loading">
    <LoadingSpinner size="3em" />
  </div>
{/snippet}

{#snippet message(message: Snippet, title: string = 'Preview Not Available')}
  <div class="message-background">
    <div class="container">
      <h2>{title}</h2>

      <div class="message">
        {@render message()}
      </div>
    </div>
  </div>
{/snippet}

{#await $promise}
  {@render loading()}
{:then { file, mime, fileData, virus, me }}
  {#if $isDesktop}
    <FileBrowserActions current={{ type: 'file', file, path: [], mime, me }} {selectedFileIds} />
  {/if}

  {#if (virus.Viruses?.length ?? 0) > 0}
    {#snippet msg()}
      <p>Virus detected. The site has prevented you from opening this file.</p>

      <p>
        {(virus.Viruses ?? [])?.join(', ')}
      </p>
    {/snippet}

    {@render message(msg)}
  {:else if mime.startsWith('image/') || mime.startsWith('video/') || mime.startsWith('text/') || mime.startsWith('audio/') || mime === 'application/pdf'}
    <FileBrowserFileContentView {fileData} {file} {mime} />
  {:else}
    {#snippet msg()}
      <p>No preview is available for this file.</p>
    {/snippet}
    {@render message(msg)}
  {/if}
{/await}

<style lang="scss">
  @use '../../../global.scss' as *;

  div.loading {
    flex-grow: 1;

    align-items: center;
    justify-content: center;
  }

  div.message-background {
    flex-grow: 1;

    align-items: safe center;
    justify-content: safe center;

    background-color: gray;

    overflow: auto;

    > div.container {
      background-color: var(--color-9);
      color: var(--color-1);

      padding: 16px;
      gap: 16px;

      > h2 {
        font-weight: bolder;
      }

      > div.message {
      }
    }
  }
</style>
