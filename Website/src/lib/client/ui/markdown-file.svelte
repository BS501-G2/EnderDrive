<script lang="ts">
  import Markdown, { type Plugin } from 'svelte-exmarkdown'

  const { source, plugins }: { source: URL | string; plugins?: Plugin[] } = $props()

  async function load(path: URL | string): Promise<string> {
    const response = await fetch(source, {})

    return await response.text()
  }
</script>

{#await load(source) then md}
  <Markdown {md} {plugins} />
{/await}
