<script lang="ts">
  import { useServerContext } from '$lib/client/client'
  import { useAppContext } from '$lib/client/contexts/app'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import { writable, type Writable } from 'svelte/store'
  import FileBrowserActions from './file-browser-actions.svelte'
  import FileBrowserFileContentView from './file-browser-file-content-view.svelte'

  const {
    fileId,
    selectedFileIds
  }: {
    fileId: string
    selectedFileIds: Writable<string[]>
  } = $props()
  const {
    getFileContents,
    getFileSnapshots,
    getLatestFileSnapshot,
    getFile,
    scanFile,
    getFileMime,
    me
  } = useServerContext()
  const { isDesktop } = useAppContext()

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

  let promise = writable(load())
</script>

{#snippet loading()}
  <div class="loading">
    <LoadingSpinner size="3em" />
  </div>
{/snippet}

{#await $promise}
  {@render loading()}
{:then { file, mime, fileContent, fileSnapshot, virusResult, me }}
  {#if $isDesktop}
    <FileBrowserActions current={{ type: 'file', file, path: [], mime, me }} {selectedFileIds} />
  {/if}

  {#if virusResult.viruses.length > 0}
    <div class="message">
      <p>Virus detected. The site has prevented you from opening this file.</p>

      <p>
        {virusResult.viruses.join(', ')}
      </p>
    </div>
  {:else if mime.startsWith('image/') || mime.startsWith('video/') || mime.startsWith('text/') || mime.startsWith('audio/') || mime === 'application/pdf'}
    <FileBrowserFileContentView {fileContent} {fileSnapshot} {file} {mime} />
  {:else}
    <div class="message">
      <p>
        File type not supported.
      </p>
    </div>
  {/if}
{/await}

<style lang="scss">
  div.loading {
    flex-grow: 1;

    align-items: center;
    justify-content: center;
  }

  div.message{
    flex-grow: 1;

    align-items: center;
    justify-content: center;
  }
</style>
