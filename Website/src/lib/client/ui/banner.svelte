<script
  lang="ts"
>
  import type { Snippet } from 'svelte';
  import type { IconOptions } from './icon.svelte';
  import Icon from './icon.svelte';

  const {
    type,
    icon,
    children,
    bottom
  }: {
    type:
      | 'error'
      | 'warning'
      | 'info';
    icon: IconOptions;
    children: Snippet;
    bottom?: Snippet;
  } =
    $props();
</script>

<div
  class="banner {type}"
>
  <div
    class="icon"
  >
    <Icon
      {...icon}
    />
  </div>
  <div
    class="content"
  >
    <div
      class="main"
    >
      {@render children()}
    </div>

    {#if bottom != null}
      <div
        class="separator"
      ></div>

      <div
        class="bottom"
      >
        {@render bottom()}
      </div>
    {/if}
  </div>
</div>

<style
  lang="scss"
>
  @use '../../../global.scss'
    as *;

  div.banner {
    flex-direction: row;

    padding: 8px;

    color: var(
      --color-5
    );

    text-align: start;
  }

  div.content {
    gap: 8px;
  }

  div.separator {
    @include force-size(
      &,
      1px
    );

    background-color: var(
      --color-5
    );
  }

  div.banner.error {
    background-color: var(
      --color-6
    );
  }

  div.banner.warning {
    background-color: var(
      --color-7
    );
  }

  div.banner.info {
    background-color: var(
      --color-8
    );
  }
</style>
