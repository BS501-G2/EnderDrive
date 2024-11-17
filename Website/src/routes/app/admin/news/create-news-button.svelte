<script
  lang="ts"
>
  import { useServerContext } from '$lib/client/client';
  import { useAppContext } from '$lib/client/contexts/app';
  import Button from '$lib/client/ui/button.svelte';
  import type { IconOptions } from '$lib/client/ui/icon.svelte';
  import { type Snippet } from 'svelte';
  import AppButton from '../../app-button.svelte';
  import AdminSidePanel from '../admin-side-panel.svelte';
  import Overlay from '../../../overlay.svelte';

  const {
    isMobile,
    isDesktop
  } =
    useAppContext();

  const icon: IconOptions =
    {
      icon: 'plus',
      thickness:
        'solid'
    };
  const {
    onopen
  }: {
    onopen: () => void;
  } =
    $props();
</script>

{#if $isDesktop}
  {#snippet foreground(
    view: Snippet
  )}
    <div
      class="foreground"
    >
      {@render view()}
    </div>
  {/snippet}

  <AdminSidePanel
    name="Create"
    {icon}
  >
    <div
      class="panel"
    >
      <p
      >
        Create
        and
        display
        news
        banners
        to
        users
        upon
        login.
      </p>

      <Button
        onclick={onopen}
        {foreground}
      >
        <p
        >
          Create
        </p>
      </Button>
    </div>
  </AdminSidePanel>
{/if}

{#if $isMobile}
  <AppButton
    label="Create News"
    onclick={onopen}
    {icon}
  />
{/if}

<style
  lang="scss"
>
  div.panel {
    gap: 8px;

    div.foreground {
      flex-grow: 1;

      background-color: var(
        --color-1
      );
      color: var(
        --color-5
      );

      padding: 8px;
    }
  }
</style>
