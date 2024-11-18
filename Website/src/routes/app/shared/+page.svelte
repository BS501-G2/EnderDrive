<script lang="ts">
  import FileBrowser from '../files/file-browser.svelte'
  import { FileBrowserResolveType } from '$lib/client/contexts/file-browser'
  import { writable } from 'svelte/store'
</script>

<FileBrowser resolve={writable([FileBrowserResolveType.Shared])} />
