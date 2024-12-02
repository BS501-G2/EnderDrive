<script lang="ts">
  import Paginator from '$lib/client/ui/lazy-loader.svelte'
  import { onMount } from 'svelte'

  let items: number[] = $state([])

  onMount(() => {
    for (let index = 0; index < 10; index++) {
      items.push(Math.random())
    }
  })
</script>

<div class="a">
  <Paginator
    bind:items
    load={async () => {
      await new Promise<void>((resolve) => setTimeout(resolve, 1000))

      const items: number[] = []

      for (let index = 0; index < 20; index++) {
        items.push(Math.random())
      }

      return items
    }}
    horizontal
  >
    {#snippet itemSnippet(item, index, key)}
      <p>{Math.floor(item * 100)}</p>
    {/snippet}
  </Paginator>
</div>

<style lang="scss">
  div.a {
    flex-grow: 1;

    min-height: 0;
    min-width: 0;

    max-height: 100dvh;
    max-width: 100dvw;
  }
</style>
