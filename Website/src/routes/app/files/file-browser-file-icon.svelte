<script lang="ts">
  import Icon from '$lib/client/ui/icon.svelte'
  import type { Size } from '$lib/client/ui/loading-spinner.svelte'

  const { mime, size }: { mime: string, size: Size } = $props()
</script>

{#if mime.startsWith('image/')}
  <Icon icon="image" {size} />
{:else if mime.startsWith('video/')}
  <Icon icon="film" {size} thickness="solid" />
{:else if mime.startsWith('audio/')}
  <Icon icon="music" {size} />
{:else if mime.startsWith('text/')}
  <Icon icon="text" {size} />
{:else if mime.startsWith('application/')}
  <Icon icon="file" {size} />
{:else}
  <Icon icon="file" {size} />
{/if}
