<script lang="ts">
  import { type Snippet } from 'svelte'
  import Overlay from '../../overlay.svelte'
  import Button from '$lib/client/ui/button.svelte'
  import Icon from '$lib/client/ui/icon.svelte'
  import type { FileBrowserAction } from '$lib/client/contexts/file-browser'
  import Separator from '$lib/client/ui/separator.svelte'

  const {
    element,
    actions,
    ondismiss
  }: {
    element: HTMLButtonElement
    actions: FileBrowserAction[]
    ondismiss: () => void
  } = $props()

  const bound = element.getBoundingClientRect()

  const x = -1
  const y = -(1 + (window.innerHeight - bound.y))
</script>

<Overlay {ondismiss} nodim {x} {y}>
  {#each actions as { id, label, icon, onclick }, index (id)}
    {#snippet foreground(view: Snippet)}
      <div class="extra-foreground">
        {@render view()}
      </div>
    {/snippet}
    {#snippet background(view: Snippet)}
      <div class="extra-background">
        {@render view()}
      </div>
    {/snippet}

    {#if index !== 0}
      <Separator horizontal />
    {/if}
    <Button {background} {foreground} {onclick}>
      <Icon {...icon} size="1.2em" />

      <p class="label">
        {label}
      </p>
    </Button>
  {/each}
</Overlay>

<style lang="scss">
  div.extra-background {
    background-color: var(--color-9);
  }

  div.extra-foreground {
    flex-direction: row;

    padding: 16px;
    gap: 16px;
  }

  p.label {
    flex-grow: 1;
  }
</style>
