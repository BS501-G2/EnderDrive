<script
  lang="ts"
>
  import { goto } from '$app/navigation';
  import { page } from '$app/stores';
  import { useServerContext } from '$lib/client/client';
  import Button from '$lib/client/ui/button.svelte';
  import Icon, {
    type IconOptions
  } from '$lib/client/ui/icon.svelte';
  import Input from '$lib/client/ui/input.svelte';
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte';
  import RequireClient from '$lib/client/ui/require-client.svelte';
  import {
    onMount,
    type Snippet
  } from 'svelte';
  import {
    writable,
    derived,
    type Writable
  } from 'svelte/store';

  const {
    username,
    password
  }: {
    username: Writable<string>;
    password: Writable<string>;
  } =
    $props();

  // let clickButton: () => void = $state(() => {});

  interface AlternativeAction {
    id: number;
    name: string;
    icon: IconOptions;
    onclick: () => Promise<void>;
  }

  const actions: Writable<
    AlternativeAction[]
  > =
    writable(
      []
    );
  function pushAction(
    name: string,
    icon: IconOptions,
    onclick: () => Promise<void>
  ): () => void {
    const id =
      Math.random();

    actions.update(
      (
        actions
      ) => [
        ...actions,
        {
          id,
          name,
          icon,
          onclick
        }
      ]
    );

    return () =>
      actions.update(
        (
          actions
        ) =>
          actions.filter(
            (
              action
            ) =>
              action.id !==
              id
          )
      );
  }

  const redirectPath =
    derived(
      page,
      (
        page
      ) =>
        page.url.searchParams.get(
          'return'
        ) ??
        null
    );

  async function redirect() {
    await goto(
      $redirectPath ??
        '/app'
    );
  }

  onMount(
    () =>
      pushAction(
        'Google',
        {
          brand: true,
          icon: 'google'
        },
        async () => {}
      )
  );
  onMount(
    () =>
      pushAction(
        'Reset Password',
        {
          thickness:
            'solid',
          icon: 'key'
        },
        async () => {}
      )
  );

  const {
    authenticatePassword,
    authenticateGoogle,
    resolveUsername,
    me,
    getUser,
    deauthenticate
  } =
    useServerContext();

  async function onclick() {
    const userId =
      await resolveUsername(
        $username
      );

    if (
      userId ==
      null
    ) {
      throw new Error(
        'Inavlid username or password.'
      );
    }

    await authenticatePassword(
      userId,
      $password
    );
    await redirect();
  }

  let click =
    $state(
      () => {}
    );
</script>

<RequireClient
>
  {#await me()}
    <LoadingSpinner
      size="3em"
    />
  {:then user}
    {#if user == null}
      <div
        class="field"
      >
        <Input
          icon={{
            icon: 'user',
            thickness:
              'solid'
          }}
          id="username"
          type="text"
          name="Username"
          bind:value={$username}
          onsubmit={click}
        />

        <Input
          icon={{
            icon: 'key',
            thickness:
              'solid'
          }}
          id="password"
          type="password"
          name="Password"
          bind:value={$password}
          onsubmit={click}
        />

        {#snippet background(
          view: Snippet
        )}
          <div
            class="submit-outer"
          >
            {@render view()}
          </div>
        {/snippet}

        {#snippet foreground(
          view: Snippet
        )}
          <div
            class="submit"
          >
            {@render view()}
          </div>
        {/snippet}

        <Button
          {background}
          {foreground}
          bind:click
          {onclick}
        >
          <p
          >
            Login
          </p>
        </Button>
      </div>

      <div
        class="choices"
      >
        <div
          class="or"
        >
          <div
            class="line"
          ></div>
          <p
          >
            or
          </p>
          <div
            class="line"
          ></div>
        </div>

        <div
          class="actions"
        >
          {#snippet background(
            view: Snippet
          )}
            <div
              class="action-container-outer"
            >
              {@render view()}
            </div>
          {/snippet}

          {#snippet foreground(
            view: Snippet
          )}
            <div
              class="action-container"
            >
              {@render view()}
            </div>
          {/snippet}

          {#each $actions as { id, name, icon, onclick } (id)}
            <Button
              {background}
              {foreground}
              {onclick}
            >
              <div
                class="action"
              >
                <Icon
                  {...icon}
                />
                <p
                >
                  {name}
                </p>
              </div>
            </Button>
          {/each}
        </div>
      </div>
    {:else}
      <div
        class="existing-login"
      >
        {#snippet background(
          view: Snippet
        )}
          <div
            class="main-button"
          >
            {@render view()}
          </div>
        {/snippet}
        {#snippet foreground(
          view: Snippet
        )}
          <p
            class="main-button"
          >
            {@render view()}
          </p>
        {/snippet}

        <Button
          {background}
          {foreground}
          onclick={() =>
            redirect()}
        >
          Proceed
          as
          @{user.username}
        </Button>
        <Button
          {background}
          {foreground}
          onclick={async () => {
            await deauthenticate();

            window.location.reload();
          }}
        >
          Log
          Out
        </Button>
      </div>
    {/if}
  {/await}
</RequireClient>

<style
  lang="scss"
>
  @use '../../global.scss'
    as *;

  div.field {
    gap: 16px;

    justify-content: safe
      center;

    div.submit-outer {
      background-color: var(
        --color-1
      );
      color: var(
        --color-5
      );
      flex-grow: 1;
    }

    div.submit {
      min-height: 32px;

      align-items: center;
      justify-content: center;
    }
  }

  div.choices {
    gap: 16px;

    > div.or {
      flex-direction: row;
      align-items: center;

      gap: 8px;

      div.line {
        flex-grow: 1;

        @include force-size(
          &,
          1px
        );

        background-color: var(
          --color-1
        );
      }
    }

    > div.actions {
      display: grid;
      gap: 8px;

      grid-template-columns: repeat(
        2,
        calc(
          50% -
            4px
        )
      );

      div.action-container-outer {
        background-color: transparent;
        color: var(
          --color-1
        );
        border: solid
          1px
          var(
            --color-1
          );

        flex-grow: 1;
      }

      div.action-container {
        padding: 8px;
      }

      div.action {
        flex-grow: 1;
        flex-direction: row;
        align-items: center;

        gap: 8px;

        > p {
          flex-grow: 1;
        }
      }
    }
  }

  div.existing-login {
    gap: 8px;

    div.main-button {
      flex-grow: 1;
      background-color: var(
        --color-1
      );
      color: var(
        --color-5
      );
    }

    p.main-button {
      padding: 8px;
    }
  }
</style>
