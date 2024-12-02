<script lang="ts">
  import Button from '$lib/client/ui/button.svelte'
  import Separator from '$lib/client/ui/separator.svelte'
  import type { Snippet } from 'svelte'
  import { createAccountSettingsContext } from './account-settings'
  import Icon from '$lib/client/ui/icon.svelte'
  import { useAppContext } from '$lib/client/contexts/app'
  import AccountSettingsPersonalInformationTab from './account-settings-personal-information-tab.svelte'
  import AccountSettingsLoginInformationTab from './account-settings-login-information-tab.svelte'

  const { tabs, currentTab } = createAccountSettingsContext()
  const { isMobile, isDesktop } = useAppContext()
</script>

<div class="page" class:desktop={$isDesktop}>
  {#if $isDesktop}
    <div class="header">
      <h2>Account Settings</h2>
    </div>
  {/if}

  <Separator horizontal />

  <div class="content" class:mobile={$isMobile} class:desktop={$isDesktop}>
    <Separator vertical />
    <div class="tabs" class:mobile={$isMobile} class:desktop={$isDesktop}>
      {#each $tabs as { id, name, icon }, index (id)}
        {#snippet tabForeground(view: Snippet)}
          <div class="tab-button" class:desktop={$isDesktop} class:active={$currentTab === index}>
            {@render view()}
          </div>
        {/snippet}

        <div class="tab-button-container">
          <Button
            foreground={tabForeground}
            onclick={async () => {
              $currentTab = index
            }}
          >
            <Icon {...icon} />
            <p>{name}</p>
          </Button>

          {#if index === $currentTab && $isMobile}
            <div class="indicator"></div>
          {/if}
        </div>
      {/each}
    </div>

    <Separator vertical />

    <div class="tab-content">
      {@render $tabs[$currentTab]?.snippet()}
    </div>

    <Separator vertical />
  </div>

  <Separator horizontal />
</div>

<AccountSettingsPersonalInformationTab />
<AccountSettingsLoginInformationTab />

<style lang="scss">
  @use '../../../global.scss' as *;

  div.page {
    flex-grow: 1;
    min-height: 0;

    > div.header {
      > h2 {
        font-size: 2rem;
        padding: 8px;
      }
    }

    > div.content {
      flex-direction: row;

      min-height: 0;
      flex-grow: 1;

      > div.tabs {
        padding: 8px;

        div.tab-button {
          flex-grow: 1;

          flex-direction: row;
          align-items: center;

          text-align: start;

          padding: 8px;
          gap: 8px;

          transition-property: background-color, color;
          transition-duration: 0.2s;
        }

        div.tab-button.active.desktop {
          background-color: var(--color-1);
          color: var(--color-5);
        }
      }

      > div.tab-content {
        flex-grow: 1;

        padding: 8px;
        gap: 8px;
        min-height: 0;

        overflow: auto;
      }
    }

    div.tabs.desktop {
      @include force-size(256px, &);
    }

    > div.content.mobile {
      flex-direction: column;

      > div.tabs {
        flex-direction: row;

        div.tab-button-container {
          flex-grow: 1;

          div.indicator {
            @include force-size(&, 1px);

            background-color: var(--color-1);
          }
        }
      }
    }
  }

  div.page.desktop {
    padding: 64px;
  }
</style>
