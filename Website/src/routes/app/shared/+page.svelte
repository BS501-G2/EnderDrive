<script lang="ts">
  import FileBrowser from '../files/file-browser.svelte'
  import { FileBrowserResolveType } from '$lib/client/contexts/file-browser'
  import { writable } from 'svelte/store'
  import { goto } from '$app/navigation'
</script>

<FileBrowser
  resolve={writable([FileBrowserResolveType.Shared])}
  onFileId={(event, fileId) => {
    return goto('/app/files?fileId=' + fileId)
  }}
/>
