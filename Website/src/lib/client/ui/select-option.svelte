<script lang="ts" generics="T">
  import type { Snippet } from 'svelte'
  import { useSelectContext } from './select'

  const { label, value, children }: { label: string, value: T; children?: Snippet<[value: T]> } = $props()

  const { pushOption } = useSelectContext<T>()

  $effect(() => pushOption(label, value, option))
</script>

{#snippet option()}
  <div class="select-option">
    {@render children?.(value)}
  </div>
{/snippet}

<style lang="scss">
  div.select-option {
    flex-grow: 1;

    padding: 4px 8px;
  }
</style>
