<script lang="ts">
  import type { FileResource } from '$lib/client/resource'
  import Window from '$lib/client/ui/window.svelte'
  import { onMount, type Snippet } from 'svelte'
  import FileBrowserAction from './file-browser-action.svelte'
  import Button from '$lib/client/ui/button.svelte'
  import Icon from '$lib/client/ui/icon.svelte'

  const { file }: { file: FileResource } = $props()

  let showCopy = $state(false)
  let confirm = $state(false)
  let input: HTMLInputElement = $state(null as never)

  $effect(() => {
    if (input == null) {
      return
    }

    input.select()
    input.setSelectionRange(0, null)
  })
</script>

{#if showCopy}
  <Window
    title="Copy Link"
    ondismiss={() => {
      showCopy = false
    }}
  >
    <input
      bind:this={input}
      disabled
      value="{window.location.protocol}//{window.location.host}/app/files?fileId={file.Id}"
    />

    <Button
      {foreground}
      onclick={async () => {
        await navigator.clipboard.writeText(input.value)

        showCopy = false
        confirm = true
      }}
    >
      <Icon icon="share" thickness="solid" />
      <p>Copy Link</p>
    </Button>
  </Window>
{:else if confirm}
  <Window
    ondismiss={() => {
      confirm = false
    }}
    title="Linked Copied"
  >
    <p>Link has been copied to your clipboard.</p>
  </Window>
{/if}

{#snippet foreground(view: Snippet)}
  <div class="foreground">
    {@render view()}
  </div>
{/snippet}

<FileBrowserAction
  type="right"
  label="Copy Link"
  icon={{ icon: 'share', thickness: 'solid' }}
  onclick={() => {
    showCopy = true
  }}
/>

<style lang="scss">
  input {
    padding: 8px;

    border: solid var(--color-1) 1px;
  }

  div.foreground {
    flex-grow: 1;

    flex-direction: row;
    align-items: center;
    justify-content: center;

    padding: 8px;
    gap: 8px;

    border: solid 1px var(--color-1);
  }
</style>
