<script lang="ts">
  import { FileBrowserResolveType } from '$lib/client/contexts/file-browser'
  import { writable } from 'svelte/store'
  import FileBrowser from '../files/file-browser.svelte'
  import { goto } from '$app/navigation'
</script>

<FileBrowser
  resolve={writable([FileBrowserResolveType.Starred])}
  onFileId={(event, fileId) => {
    return goto('/app/files?fileId=' + fileId)
  }}
/>
