<script lang="ts">
  import { useServerContext } from '$lib/client/client'
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
  }:
 {
    fileId: string
    selectedFileIds: Writable<string[]>
  } = $props()
  const {
    getFileContents,
    getFileDataList: getFileSnapshots,
    getLatestFileSnapshot,
    getFile,
    scanFile,
    getFileMime,
    me
  } = useServerContext()
  const { isDesktop } = useAppContext()
  const snapshotId = derived(page, ({ url }) => url.searchParams.get('snapshotId'))

  async function load(customSnapshotId?: string) {
    const file = await getFile(fileId)
    const self = await me()

    const mime = await getFileMime(fileId)
    const fileContent = (await getFileContents(fileId, void 0, 0, 1))[0]
    const fileSnapshot =
      customSnapshotId != null
        ? (await getFileSnapshots(file.id, fileContent.id, customSnapshotId, 0, 1))[0]
        : (await getLatestFileSnapshot(file.id, fileContent.id))!

    const virusResult = await scanFile(fileId, fileContent.id, fileSnapshot.id)

    return {
      file,
      mime,
      fileContent,
      fileSnapshot,
      virusResult,
      me: self
    }
  }

  let promise = writable(load($snapshotId || undefined))
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
{:then { file, mime, fileContent, fileSnapshot, virusResult, me }}
  {#if $isDesktop}
    <FileBrowserActions current={{ type: 'file', file, path: [], mime, me }} {selectedFileIds} />
  {/if}

  {#if virusResult.viruses.length > 0}
    {#snippet msg()}
      <p>Virus detected. The site has prevented you from opening this file.</p>

      <p>
        {virusResult.viruses.join(', ')}
      </p>
    {/snippet}

    {@render message(msg)}
  {:else if mime.startsWith('image/') || mime.startsWith('video/') || mime.startsWith('text/') || mime.startsWith('audio/') || mime === 'application/pdf'}
    <FileBrowserFileContentView {fileContent} {fileSnapshot} {file} {mime} />
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
