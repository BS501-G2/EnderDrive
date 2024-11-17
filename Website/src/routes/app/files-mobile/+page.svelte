<script
  lang="ts"
>
  import { goto } from '$app/navigation';
  import { useAppContext } from '$lib/client/contexts/app';
  import Button from '$lib/client/ui/button.svelte';
  import Icon, {
    type IconOptions
  } from '$lib/client/ui/icon.svelte';
  import {
    onMount,
    type Snippet
  } from 'svelte';

  const {
    isDesktop
  } =
    useAppContext();

  onMount(
    () =>
      isDesktop.subscribe(
        async (
          isDesktop
        ) => {
          if (
            isDesktop
          ) {
            await goto(
              '/app/files',
              {
                replaceState: true
              }
            );
          }
        }
      )
  );
</script>

{#snippet buttons(
  icon: IconOptions,
  label: string,
  path: string
)}
  {#snippet buttonForeground(
    view: Snippet
  )}
    <div
      class="foreground"
    >
      {@render view()}
    </div>
  {/snippet}

  <div
    class="background"
  >
    <Button
      foreground={buttonForeground}
      onclick={() => {
        goto(
          path
        );
      }}
    >
      <Icon
        {...icon}
      />
      <p
        class="label"
      >
        {label}
      </p>
    </Button>
  </div>
{/snippet}

<div
  class="buttons-container"
>
  <div
    class="buttons"
  >
    {@render buttons(
      {
        icon: 'folder',
        size: '2em'
      },
      'Files',
      '/app/files'
    )}
    {@render buttons(
      {
        icon: 'user',
        size: '2em'
      },
      'Shared',
      '/app/shared'
    )}
    {@render buttons(
      {
        icon: 'star',
        size: '2em'
      },
      'Starred',
      '/app/starred'
    )}
    {@render buttons(
      {
        icon: 'trash-can',
        size: '2em'
      },
      'Trash',
      '/app/trash'
    )}
  </div>
</div>

<style
  lang="scss"
>
  @use '../../../global.scss'
    as *;

  div.buttons-container {
    flex-grow: 1;

    justify-content: center;
  }

  div.buttons {
    display: flex;
    flex-direction: row;
    flex-wrap: wrap;
    align-items: center;

    padding: 16px;
    gap: 16px;
  }

  div.background {
    @include force-size(
      calc(
        50% -
          16px
      ),
      &
    );

    box-sizing: border-box;

    flex-shrink: 0;

    > :global(
        button
      ) {
      flex-grow: 1;
    }
  }

  div.foreground {
    flex-direction: column;
    flex-grow: 1;

    align-items: center;

    border-radius: 16px;
    box-shadow: 2px
      2px
      4px
      var(
        --color-10
      );

    padding: 16px;
    margin: 8px;
    gap: 8px;

    p.label {
      font-size: 1.5em;
    }
  }
</style>
