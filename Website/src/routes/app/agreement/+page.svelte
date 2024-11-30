<script lang="ts">
  import Button from '$lib/client/ui/button.svelte'
  import Window from '$lib/client/ui/window.svelte'
  import { onMount, type Snippet } from 'svelte'
  import { writable, type Writable } from 'svelte/store'

  import { useClientContext } from '$lib/client/client'
  import { goto } from '$app/navigation'
  import { page } from '$app/stores'
  import MarkdownFile from '$lib/client/ui/markdown-file.svelte'

  const {}: {} = $props()

  const {server} = useClientContext()

  async function load(): Promise<void> {
    if ((await server.DidIAgree({}))) {
      goto($page.url.searchParams.get('return') ?? '/app')
    }
  }

  onMount(() => {
    load()
  })

  const promise: Writable<Promise<void>> = writable(load())
  const checked = writable<boolean>(false)
</script>

<Window ondismiss={() => {}} title="Privacy Policy Agreement for EnderDrive">
  {#key $promise}
    <div class="agreement">
      <p>This privacy policy was last updated: November 1, 2024</p>

      <div class="main">
        <MarkdownFile source="/agreement.md" />
      </div>
      <div class="footer">
        <div class="field">
          <input id="agree" type="checkbox" bind:checked={$checked} />
          <label for="agree" class="label">I agree to the privacy policy stated above.</label>
        </div>

        {#snippet foreground(view: Snippet)}
          <div class="foreground" class:disabled={!$checked}>
            {@render view()}
          </div>
        {/snippet}

        <Button
          disabled={!$checked}
          {foreground}
          onclick={async () => {
            await server.Agree({})
            await ($promise = load())
          }}
        >
          Proceed
        </Button>
      </div>
    </div>
  {/key}
</Window>

<style lang="scss">
  @use '../../../global.scss' as *;

  div.agreement {
    @include force-size(min(840px, 100dvw), min(540px, 100dvh));

    gap: 8px;

    div.main {
      flex-grow: 1;

      border: solid 1px var(--color-1);
      padding: 8px;

      overflow: auto;

      display: block;

      :global(*) {
        all: revert;
      }
    }

    div.footer {
      flex-direction: row;

      gap: 8px;

      > div.field {
        flex-grow: 1;

        flex-direction: row;

        align-items: center;
      }

      div.foreground {
        padding: 8px;

        background-color: var(--color-1);
        color: var(--color-5);

        transition-property: background-color, color;
        transition-duration: 0.2s;
      }

      div.disabled {
        background-color: unset;
        color: inherit;
      }
    }
  }
</style>
