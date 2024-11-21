<script lang="ts">
  import type { Snippet } from 'svelte'
  import Overlay from '../../../routes/overlay.svelte'
  import Separator from './separator.svelte'
  import type { IconOptions } from './icon.svelte'
  import Icon from './icon.svelte'

  const {
    titleIcon,
    title,
    header,
    children: body,
    ondismiss
  }: {
    titleIcon?: IconOptions
    title?: string
    header?: Snippet
    children: Snippet
    ondismiss: () => void
  } = $props()
</script>

<Overlay {ondismiss}>
  {#snippet children(windowButtons: Snippet)}
    <div class="window">
      <div class="header">
        <div class="title">
          {#if titleIcon}
            <div class="icon">
              <Icon {...titleIcon} size="1rem" />
            </div>
          {/if}

          <h2 class="title">
            {title ?? ''}
          </h2>

          {#if header}
            <div class="head-bar">
              {@render header()}
            </div>
          {/if}
        </div>

        {@render windowButtons()}
      </div>

      <Separator horizontal />

      <div class="body">
        {@render body()}
      </div>
    </div>
  {/snippet}
</Overlay>

<style lang="scss">
  div.window {
    background-color: var(--color-9);
    color: var(--color-1);

    > div.header {
      gap: 16px;

      flex-direction: row;

      > div.title {
        flex-grow: 1;
        gap: 16px;
        padding: 8px 16px;

        flex-direction: row;

      align-items: center;

        > div.icon {
        }

        > h2.title {
          flex-grow: 1;
          font-size: 1.2em;
          font-weight: bolder;
        }


      }
    }

    > div.body {
      padding: 16px;
      gap: 8px;
    }
  }
</style>
