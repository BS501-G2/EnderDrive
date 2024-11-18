<script lang="ts">
  import type { Snippet } from 'svelte'
  import Overlay from '../../../routes/overlay.svelte'

  const {
    ondismiss,
    head,
    children,
    footer
  }: {
    ondismiss: () => void
    head?: Snippet
    footer?: Snippet
    children?: Snippet
  } = $props()
</script>

<Overlay {ondismiss}>
  <div class="dialog">
    {#if head}
      <div class="head">
        {@render head()}
      </div>
    {/if}

    {#if children}
      <div class="body">
        {@render children()}
      </div>
    {/if}

    {#if footer}
      <div class="footer">
        {@render footer()}
      </div>
    {/if}
  </div>
</Overlay>

<style lang="scss">
  div.dialog {
    display: flex;
    flex-direction: column;

    padding: 8px;

    box-shadow: 2px 2px 8px black;
    background-color: white;

    padding: 16px;
    gap: 16px;

    > div.head,
    > div.footer,
    > div.body {
      gap: 8px;
    }

    > div.head,
    > div.footer {
      flex-direction: row;
    }
  }
</style>
