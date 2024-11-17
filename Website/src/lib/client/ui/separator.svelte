<script
  lang="ts"
>
  const {
    ...props
  }:
    | {
        horizontal: true;
      }
    | {
        vertical: true;
      } =
    $props();
</script>

{#if 'horizontal' in props}
  <div
    class="separator horizontal"
  ></div>
{:else if 'vertical' in props}
  <div
    class="separator vertical"
  ></div>
{/if}

<style
  lang="scss"
>
  @use '../../../global.scss'
    as *;

  div.separator {
    align-self: stretch;

    background-color: var(
      --color-5
    );
  }

  div.separator.vertical {
    @include force-size(
      1px,
      &
    );
  }

  div.separator.horizontal {
    @include force-size(
      &,
      1px
    );
  }
</style>
