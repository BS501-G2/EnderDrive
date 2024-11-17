<script
  lang="ts"
>
  import { goto } from '$app/navigation';
  import { page } from '$app/stores';
  import { useServerContext } from '$lib/client/client';
  import { useAppContext } from '$lib/client/contexts/app';
  import Button from '$lib/client/ui/button.svelte';
  import Icon from '$lib/client/ui/icon.svelte';
  import Input from '$lib/client/ui/input.svelte';
  import {
    onMount,
    type Snippet
  } from 'svelte';

  const {
    getSetupRequirements,
    createAdmin
  } =
    useServerContext();
  const {
    isMobile
  } =
    useAppContext();

  onMount(
    async () => {
      const response =
        await getSetupRequirements();

      if (
        !response.adminSetupRequired
      ) {
        await goto(
          '/'
        );
      }
    }
  );

  let pages: {
    id: number;
    title: string;
    snippet: Snippet;
  }[] =
    $state(
      []
    );
  let setup: number =
    $state(
      0
    );

  let username: string =
    $state(
      ''
    );
  let password: string =
    $state(
      ''
    );
  let confirmPassword: string =
    $state(
      ''
    );
  let firstName: string =
    $state(
      ''
    );
  let middleName: string =
    $state(
      ''
    );
  let lastName: string =
    $state(
      ''
    );
  let displayName: string =
    $state(
      ''
    );

  function pushPage(
    title: string,
    snippet: Snippet
  ) {
    const id =
      Date.now();
    pages =
      [
        ...pages,
        {
          id,
          title,
          snippet
        }
      ];

    return () => {
      pages =
        pages.filter(
          (
            page
          ) =>
            page.id !==
            id
        );
    };
  }

  async function finalize() {
    await createAdmin(
      username,
      password,
      confirmPassword,
      firstName,
      middleName ||
        null,
      lastName,
      displayName ||
        null
    );

    await goto(
      $page.url.searchParams.get(
        'return'
      ) ??
        '/'
    );
  }

  onMount(
    () =>
      pushPage(
        'First-time Setup',
        step0
      )
  );
  onMount(
    () =>
      pushPage(
        'Administrator Account',
        step1
      )
  );
  onMount(
    () =>
      pushPage(
        'Final step',
        step2
      )
  );
</script>

{#snippet background(
  view: Snippet,
  error: boolean,
  secondary: boolean = false
)}
  <div
    class="nav-button"
    class:secondary
    class:error
  >
    {@render view()}
  </div>
{/snippet}

{#snippet backgroundSecondary(
  view: Snippet,
  error: boolean
)}
  {@render background(
    view,
    error,
    true
  )}
{/snippet}

{#snippet foreground(
  view: Snippet
)}
  <p
    class="nav-label"
  >
    {@render view()}
  </p>
{/snippet}

<div
  class="page-container"
>
  <div
    class="page"
  >
    <div
      class="header"
    >
      {#if $isMobile && pages[setup - 1] != null}
        <Button
          onclick={() => {
            setup--;
          }}
          background={backgroundSecondary}
          {foreground}
        >
          <Icon
            icon="chevron-left"
            thickness="solid"
          />
        </Button>
      {/if}
      <h2
      >
        {#if pages[setup] != null}
          {pages[
            setup
          ]
            .title}
        {/if}
      </h2>
      {#if $isMobile && pages[setup + 1] != null}
        <Button
          onclick={() => {
            setup++;
          }}
          {background}
          {foreground}
        >
          <Icon
            icon="chevron-right"
            thickness="solid"
          />
        </Button>
      {/if}
    </div>

    <div
      class="separator"
    ></div>

    <div
      class="body"
    >
      {#if pages[setup] != null}
        {@render pages[
          setup
        ].snippet()}
      {/if}
    </div>

    <div
      class="separator"
    ></div>

    {#if !$isMobile}
      <div
        class="bottom"
      >
        <div
          class="steps"
        >
          <p
          >
            {pages[
              setup
            ]
              ?.title}
          </p>

          <div
            class="lines"
          >
            {#each Array(pages.length) as { }, index (index)}
              <div
                class="line"
                class:active={index ==
                  setup}
              ></div>
            {/each}
          </div>
        </div>
        <div
          class="buttons"
        >
          {#if pages[setup - 1] != null}
            <Button
              onclick={() => {
                setup--;
              }}
              background={backgroundSecondary}
              {foreground}
            >
              Previous
            </Button>
          {/if}

          {#if pages[setup + 1] == null}
            <Button
              onclick={() =>
                finalize()}
              {background}
              {foreground}
              >Finalize</Button
            >
          {:else}
            <Button
              onclick={() => {
                setup++;
              }}
              disabled={pages[
                setup +
                  1
              ] ==
                null}
              {background}
              {foreground}
            >
              Next
            </Button>
          {/if}
        </div>
      </div>
    {/if}
  </div>
</div>

{#snippet step0()}
  <p
  >
    This
    website
    requires
    a
    first-time
    setup.
    We
    need
    you
    to
    create
    the
    first
    administrator
    account
    to
    kickstart
    the
    website
    operations.
  </p>

  <p
  >
    In
    the
    subsequent
    steps,
    you
    will
    be
    guided
    to
    create
    an
    account
    with
    full
    administrator
    privileges.
  </p>
{/snippet}

{#snippet step1()}
  <h2
    class="field-header"
  >
    Credentials
  </h2>
  <div
    class="fields"
  >
    <div
      class="padding"
    >
      <Input
        id="username"
        type="text"
        name="Username"
        bind:value={username}
      />
    </div>
    <div
      class="padding"
    >
      <Input
        id="display-name"
        type="text"
        name="Display Name"
        bind:value={displayName}
      />
    </div>
    <div
      class="padding"
    >
      <Input
        id="password"
        type="password"
        name="Password"
        bind:value={password}
      />
    </div>
    <div
      class="padding"
    >
      <Input
        id="confirm-password"
        type="password"
        name="Confirm Password"
        bind:value={confirmPassword}
      />
    </div>
  </div>

  <h2
    class="field-header"
  >
    Personal
    Information
  </h2>
  <div
    class="fields"
  >
    <div
      class="padding"
    >
      <Input
        id="first-name"
        type="text"
        name="First Name"
        bind:value={firstName}
      />
    </div>
    <div
      class="padding"
    >
      <Input
        id="middlen-name"
        type="text"
        name="Middle Name"
        bind:value={middleName}
      />
    </div>
    <div
      class="padding"
    >
      <Input
        id="last-name"
        type="text"
        name="Last Name"
        bind:value={lastName}
      />
    </div>
  </div>
{/snippet}

{#snippet step2()}
  <p
  >
    Click
    proceed
    to
    create
    an
    administrator
    account.
  </p>
{/snippet}

<style
  lang="scss"
>
  @use '../../global.scss'
    as *;

  div.page-container {
    flex-grow: 1;
    align-items: center;
    min-height: 0;
    max-height: 100dvh;

    div.page {
      @include force-size(
        min(
          1280px,
          100dvw
        ),
        &
      );

      padding: 32px;
      flex-grow: 1;
      gap: 32px;
      box-sizing: border-box;
      min-height: 0;

      > div.header {
        flex-direction: row;
        align-items: center;
        gap: 8px;

        > h2 {
          font-size: 2em;

          flex-grow: 1;
        }
      }

      > div.separator {
        background-color: var(
          --color-1
        );

        @include force-size(
          &,
          1px
        );
      }

      > div.body {
        flex-grow: 1;
        gap: 8px;
        overflow: hidden
          auto;
        min-height: 0;

        div.fields {
          display: grid;

          justify-content: space-evenly;
          grid-template-columns: repeat(
            auto-fit,
            minmax(
              0,
              min(
                100%/1,
                max(
                  360px,
                  100%/2
                )
              )
            )
          );

          > div.padding {
            padding: 8px;
          }
        }
      }

      > div.bottom {
        flex-direction: row;

        padding: 16px
          0;

        > div.steps {
          flex-grow: 1;
          justify-content: space-between;

          > p {
            font-weight: lighter;
          }

          div.lines {
            gap: 4px;
            flex-direction: row;
            flex-wrap: wrap;

            div.line {
              background-color: var(
                --color-9
              );

              @include force-size(
                32px,
                1px
              );
            }

            div.line.active {
              background-color: var(
                --color-1
              );
            }
          }
        }

        > div.buttons {
          flex-direction: row;
          gap: 8px;
        }
      }
    }
  }

  div.nav-button {
    background-color: var(
      --color-1
    );
    color: var(
      --color-5
    );
  }

  div.nav-button.secondary {
    background-color: transparent;
    color: var(
      --color-1
    );
  }

  div.nav-button.error {
    background-color: var(
      --color-6
    );
  }

  p.nav-label {
    padding: 8px;
  }
</style>
