<script lang="ts">
  import type { FileProperties } from '$lib/client/contexts/file-browser'
  import Button from '$lib/client/ui/button.svelte'
  import Icon, { type IconOptions } from '$lib/client/ui/icon.svelte'
  import Separator from '$lib/client/ui/separator.svelte'
  import { onMount, type Snippet } from 'svelte'
  import { writable, type Writable } from 'svelte/store'
  import { useAppContext } from '$lib/client/contexts/app'

  const {
    files
  }: {
    files: FileProperties[]
  } = $props()

  const { isMobile } = useAppContext()
  const tabs: Writable<
    {
      id: number
      name: string
      icon: IconOptions
      content: Snippet
    }[]
  > = writable([])
  const currentTab: Writable<number> = writable(0)

  function pushTab(name: string, icon: IconOptions, content: Snippet) {
    const id = Math.random()

    tabs.update((value) => [
      ...value,
      {
        id,
        name,
        icon,
        content
      }
    ])

    return () => tabs.update((value) => value.filter((tab) => tab.id !== id))
  }

  onMount(() =>
    pushTab(
      'Details',
      {
        icon: 'info',
        thickness: 'solid'
      },
      details
    )
  )

  onMount(() =>
    pushTab(
      'Logs',
      {
        icon: 'book',
        thickness: 'solid'
      },
      logs
    )
  )

  onMount(() =>
    pushTab(
      'Access',
      {
        icon: 'key',
        thickness: 'solid'
      },
      logs
    )
  )
</script>

{#snippet details()}
  <div class="overview">
    {#snippet field(name: string, value: Snippet | string)}
      <div class="row">
        <p class="label">{name}</p>
        <p class="value">
          {#if typeof value === 'string'}
            {value}
          {:else}
            {@render value()}
          {/if}
        </p>
      </div>
    {/snippet}

    {#if files.length === 1}
      {@render field('File Type', files[0].mime)}
      {@render field('Created', `${files[0].created}`)}
      {@render field('Modified', `${files[0].modified}`)}
    {/if}
  </div>
{/snippet}

{#snippet logs()}
  <div class="logs"></div>
{/snippet}

{#snippet accesses()}
  <div class="accesses"></div>
{/snippet}

<div class="details">
  <div class="tabs">
    {#each $tabs as { id, name, icon }, index (id)}
      {#snippet foreground(view: Snippet)}
        <div class="tab-button">
          {@render view()}
        </div>
      {/snippet}

      <div class="entry">
        <Button {foreground} onclick={() => currentTab.set(index)}>
          <Icon {...icon} />
          <p class="label">
            {name}
          </p>
        </Button>

        {#if $currentTab === index}
          <div class="indicator" class:mobile={$isMobile}></div>
        {/if}
      </div>
    {/each}
  </div>

  <Separator horizontal />

  <div class="tab-content">
    {@render $tabs[$currentTab]?.content()}
  </div>
</div>

<style lang="scss">
  @use '../../../global.scss' as *;

  div.details {
    flex-grow: 1;

    > div.tabs {
      flex-direction: row;
      overflow: auto hidden;

      div.entry {
        flex-grow: 1;

        div.tab-button {
          flex-direction: row;
          align-items: center;

          padding: 8px;
          gap: 8px;

          p.label {
          }
        }

        div.indicator {
          @include force-size(&, 1px);

          background-color: var(--color-1);
        }

        div.indicator.mobile {
          background-color: var(--color-5);
        }
      }
    }

    > div.tab-content {
      flex-grow: 1;

      div.overview {
        flex-grow: 1;

        padding: 8px;
        gap: 8px;

        > div.row {
          flex-direction: row;

          gap: 4px;

          > p.label {
            @include force-size(96px, &);

            text-align: end;
            font-weight: bolder;
          }

          > p.label::after {
            content: ':';
          }

          > p.value {
            flex-grow: 1;
          }
        }
      }

      div.logs {
      }
    }
  }
</style>
