<script lang="ts">
  import { FileBrowserResolveType } from '$lib/client/contexts/file-browser'
  import { writable } from 'svelte/store'
  import FileBrowser from '../files/file-browser.svelte'
</script>

<FileBrowser resolve={writable([FileBrowserResolveType.Trash])} />
