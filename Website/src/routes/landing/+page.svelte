<script lang="ts">
  import { useAppContext } from '$lib/client/contexts/app'
  import { useLandingContext } from '$lib/client/contexts/landing'
  import { onMount } from 'svelte'
  import LandingAction from './landing-action.svelte'
  import LandingEntry from './landing-entry.svelte'

  const { isDesktop, isMobile } = useAppContext()
  const { pushButton } = useLandingContext()

  let scrollHeight: number = $state(0)
  let imageHeight: number = $state(0)

  onMount(() =>
    pushButton(
      downloadButton,
      {
        icon: 'download'
      },
      false,
      () => {}
    )
  )
</script>

<svelte:window bind:scrollY={scrollHeight} />

<LandingEntry name="Home" hideHeader>
  <div class="img" class:mobile={$isMobile} bind:offsetHeight={imageHeight}>
    <img
      src="/school-building.jpg"
      alt="School Building"
      style:min-height="{imageHeight}px"
    />
  </div>

  <div class="header-container" class:mobile={$isMobile}>
    <div class="header-inner-container">
      <div class="header">
        <img src="/icon.jpg" alt="School Logo" />
        <div class="info">
          <h2>Melchora Aquino Elementary School</h2>
          <p>Solis St., Tondo, Manila</p>
        </div>
      </div>
    </div>
  </div>
</LandingEntry>

<LandingEntry name="About" contain>//</LandingEntry>

<LandingEntry name="Why EnderDrive?" contain
  >The employees of Melchora Aquino Elementary School have been using
  traditional methods of storing digital data.</LandingEntry
>

<LandingEntry name="Contact" contain>//</LandingEntry>

{#snippet downloadButton()}
  <p>Download</p>
{/snippet}

<LandingAction
  onclick={() => {}}
  icon={{
    icon: 'download'
  }}
>
  <p>Download</p>
</LandingAction>

<style lang="scss">
  @use '../../global.scss' as *;

  div.img {
    @include force-size(&, min(max(360px, 75dvh), 100dvh));

    > img {
      object-fit: cover;
    }
  }

  div.img.mobile {
    @include force-size(&, 100dvh);
  }

  div.header-container {
    margin-top: -256px;
    min-height: 172px;
    margin-bottom: 72px;

    flex-direction: row;
    justify-content: center;

    padding: 0px 64px;

    color: var(--color-5);
    filter: drop-shadow(2px 2px 4px black);
    backdrop-filter: blur(4px);

    > div.header-inner-container {
      min-width: min(1280px, 100%);

      flex-direction: row;
      align-items: center;

      > div.header {
        flex-direction: row;
        align-items: center;
        gap: 64px;

        > div.info {
          gap: 16px;

          > h2 {
            font-size: 2em;
          }
        }

        > img {
          @include force-size(128px, 128px);

          border-radius: 50%;
        }
      }
    }
  }

  div.header-container.mobile {
    margin-top: -75dvh;
    margin-bottom: 35dvh;
    padding: 64px 0px;

    div.header-inner-container {
      min-width: unset;

      div.header {
        flex-direction: column;
        align-items: center;

        gap: 0px;

        text-align: center;
      }
    }
  }

  div.footer-container {
    flex-grow: 1;
    align-items: center;

    background-color: var(--color-1);
    color: var(--color-5);

    box-sizing: border-box;
    padding: 16px;

    div.footer {
      @include force-size(min(1280px, 100%), &);
    }
  }
</style>
