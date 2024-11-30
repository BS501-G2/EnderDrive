<script lang="ts">
  import { useClientContext } from '$lib/client/client';
  import type { FileProperties } from '$lib/client/contexts/file-browser'
  import { AudioTranscriptionStatus } from '$lib/client/resource'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import FileBrowserPropertiesTab from './file-browser-properties-tab.svelte'

  const { file }: { file: FileProperties } = $props()
  const { server } = useClientContext()
</script>

<FileBrowserPropertiesTab
  label="Audio Transcript"
  icon={{ icon: 'microphone', thickness: 'solid' }}
>
  {#await server.TranscribeAudio({FileId: file.file.Id})}
    <div class="loading">
      <LoadingSpinner size="3em" />
    </div>
  {:then { Text, Status }}
    <div class="transcript">
      <p><b>Status:</b> {AudioTranscriptionStatus[Status]}</p>
      {#if Status === AudioTranscriptionStatus.Error}
        <p>Audio transcription only works on WAV files using 16KHz.</p>
      {:else}
        <p><b>Transcript:</b></p>

        {#each Text as entry}
          <p>{entry}</p>
        {/each}
      {/if}
    </div>
  {/await}
</FileBrowserPropertiesTab>

<style lang="scss">
  div.loading {
    flex-grow: 1;
  }

  div.transcript {
    padding: 8px;
    gap: 8px;

    > pre {
      text-wrap: wrap;
    }
  }

  b {
    font-weight: bolder;
  }
</style>
