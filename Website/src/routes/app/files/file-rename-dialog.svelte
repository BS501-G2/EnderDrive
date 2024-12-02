<script lang="ts">
  import { useClientContext } from '$lib/client/client'
  import { FileNameValidationFlags, type FileResource } from '$lib/client/resource'
  import Button from '$lib/client/ui/button.svelte'
  import Input from '$lib/client/ui/input.svelte'
  import Window from '$lib/client/ui/window.svelte'
  import { onMount, type Snippet } from 'svelte'
  import { writable } from 'svelte/store'

  const { file, ondismiss }: { file: FileResource; ondismiss: () => void } = $props()

  const { server } = useClientContext()

  const rename = writable('')
  const errors = writable<string[]>([])

  async function check(name: string) {
    const flags = await server.GetFileNameValidationFlags({
      ParentId: file.ParentId!,
      Name: name
    })
    const newErrors: string[] = []

    if (flags & FileNameValidationFlags.FileExists) {
      newErrors.push('File exists')
    }

    if (flags & FileNameValidationFlags.InvalidCharacters) {
      newErrors.push('Invalid characters')
    }

    if (flags & FileNameValidationFlags.TooLong) {
      newErrors.push('Too Long')
    }

    if (flags & FileNameValidationFlags.TooShort) {
      newErrors.push('Too Short')
    }

    errors.set(newErrors)
  }

  onMount(() => rename.subscribe((name) => check(name)))
</script>

{#snippet foreground(view: Snippet)}
  <div class="foreground">
    {@render view()}
  </div>
{/snippet}

{#snippet background(view: Snippet)}
  <div class="background">
    {@render view()}
  </div>
{/snippet}

<Window {ondismiss} title="Rename File">
  <Input type="text" id="rename" bind:value={$rename} />

  {#each $errors as error}
    <p class="error">{error}</p>
  {/each}

  <Button
    {foreground}
    {background}
    onclick={async () => {
      await server.MoveFile({ FileId: file.Id , NewName: $rename })
    }}
  >
    Submit
  </Button>
</Window>

<style lang="scss">
  div.background {
    background-color: var(--color-1);
    color: var(--color-5);
  }

  div.foreground {
    padding: 8px;
  }

  p.error {
    color: var(--color-6);
  }
</style>
