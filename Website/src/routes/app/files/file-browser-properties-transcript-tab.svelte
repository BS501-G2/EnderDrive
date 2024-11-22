<script lang="ts">
  import { AudioTranscriptionStatus, useServerContext, type FileResource } from '$lib/client/client'
  import type { FileProperties } from '$lib/client/contexts/file-browser'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import FileBrowserPropertiesTab from './file-browser-properties-tab.svelte'

  const { file }: { file: FileProperties } = $props()
  const { transcribeAudio } = useServerContext()
</script>

<FileBrowserPropertiesTab
  label="Audio Transcript"
  icon={{ icon: 'microphone', thickness: 'solid' }}
>
  {#await transcribeAudio(file.file.id)}
    <div class="loading">
      <LoadingSpinner size="3em" />
    </div>
  {:then { text, status }}
    <div class="transcript">
      <p><b>Status:</b> {AudioTranscriptionStatus[status]}</p>
      <p><b>Transcript:</b></p>
      <pre>{text.join('\n')}</pre>
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
  }

  b {
    font-weight: bolder;
  }
</style>
