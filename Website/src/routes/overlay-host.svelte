<script lang="ts">
  import { type Snippet } from 'svelte'
  import { derived, type Readable } from 'svelte/store'
  import { fade } from 'svelte/transition'

  const {
    overlay
  }: {
    overlay: Readable<[id: number, snippet: Snippet<[]>, dim: boolean, shadow: boolean][]>
  } = $props()

  const dim = derived(overlay, (value) => value.some(([, , dim]) => dim))
</script>

{#if $overlay.length != 0}
  <div
    class="overlay"
    class:dim={$dim}
    transition:fade={{
      duration: 250
    }}
  >
    <div class:overlay-dim={$dim}>
      {#each $overlay as [id, snippet, , shadow] (id)}
        {#if shadow}
          <div class="overlay-shadow">
            {@render snippet()}
          </div>
        {:else}
          {@render snippet()}
        {/if}
      {/each}
    </div>
  </div>
{/if}

<style lang="scss">
  @use '../global.scss' as *;

  div.overlay {
    flex-direction: column;

    pointer-events: none;

    position: fixed;
    top: 0;
    left: 0;

    z-index: 1;

    @include force-size(100dvw, 100dvh);

    div.overlay-shadow {
      flex-direction: column;
      filter: drop-shadow(2px 2px 2px black);
    }

    > div.overlay-dim {
      flex-direction: column;
    }
  }
</style>
