<script lang="ts">
  import type { Snippet } from 'svelte'

  const { children }: { children?: Snippet } = $props()
</script>

<table class="table">
  {@render children?.()}
</table>

<style lang="scss">
  table.table {
    :global(td),
    :global(th) {
      align-content: center;
      justify-items: start;
    }

    :global(thead) {
      :global(th) {
        padding: 8px 0 8px 0;
        text-align: start;
      }
    }
  }
</style>
