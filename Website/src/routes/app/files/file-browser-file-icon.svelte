<script lang="ts">
  import { FileType } from '$lib/client/client'
  import Icon from '$lib/client/ui/icon.svelte'
  import type { Size } from '$lib/client/ui/loading-spinner.svelte'

  const { mime, size, type }: { mime: string, size: Size, type?: FileType } = $props()
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
{:else if type === FileType.Folder}
  <Icon icon="folder" {size} />
{:else}
  <Icon icon="file" {size} />
{/if}
