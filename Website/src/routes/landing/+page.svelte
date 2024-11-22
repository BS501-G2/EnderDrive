<script lang="ts">
  import { useAppContext } from '$lib/client/contexts/app'
  import { useLandingContext } from '$lib/client/contexts/landing'
  import { onMount } from 'svelte'
  import LandingAction from './landing-action.svelte'
  import LandingEntry from './landing-entry.svelte'
  import { writable } from 'svelte/store'

  const { isDesktop, isMobile } = useAppContext()
  const { pushButton } = useLandingContext()

  const scrollHeight = writable<number>(0)
  const imageHeight = writable<number>(0)

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

<svelte:window bind:scrollY={$scrollHeight} />

<LandingEntry name="Home" hideHeader>
  <div class="img" class:mobile={$isMobile} bind:offsetHeight={$imageHeight}>
    <img src="/school-building.jpg" alt="School Building" style:min-height="{$imageHeight}px" />
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
<LandingEntry name="Hero" hideHeader>
   <div class="firstGrid-Container" id="home">
    <div class="gridInfo1">
      <h1>Store, share, and collaborate on files and folders across your phone, tablet, or computer.</h1>
      <p>Secure and Private File Storage and Sharing Website <br> for Melchora Aquino Elementary School.</p>

      <div class="button-container1">
        <button class="btn btn-primary">Download</button>
        <button class="btn btn-secondary">Login</button>
      </div>
    </div>
    <div class="gridImage1">
      <img src="first.png" alt="first">
    </div>
  </div>
</LandingEntry>
<LandingEntry name="About" contain>//

</LandingEntry>

<LandingEntry name="Why EnderDrive?" contain
  >The employees of Melchora Aquino Elementary School have been using traditional methods of storing
  digital data.</LandingEntry
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
   div.firstGrid-Container{
    display:grid;
    place-items: center;
    grid-template-columns: auto auto;
    gap: 20px;
    padding: 50px;
  }
  div.gridInfo1{
    max-width: 100%;
    max-height: 100%;
    overflow: hidden;
    margin: auto;
  }
  div.gridImage1{
    max-width: auto;
    object-fit:cover;
    max-height: auto;
    overflow: hidden;
  }
  p{
    text-align: left;
    line-height: 1;
    font-weight: lighter;
    font-size: 25px;
    padding: 20px;
  }
  h1{
    font-size: 45px;
    word-spacing: normal;
    letter-spacing: normal;
    text-align: left;
    font-weight: bold;
    line-height: 1;
  }
  div.button-container1 {
    margin-top: 20px;
    display:grid;
    grid-template-columns: auto auto;
    justify-content: start;
  }

  .btn {
    margin-right: 10px;
    padding: 10px 20px;
    border: 2px solid transparent;
    border-radius: 5px;
    cursor: pointer;
    font-size: 20px;
    transition: border-color 0.5s, box-shadow 0.5s;
  }
  .btn-primary {
    background-color: rgb(185, 188, 188);
    color: black;
  }
  .btn-secondary {
    background-color: rgb(12, 91, 66);
    color: white;
  }
  .btn:hover {
    background-color: #0f3031ff;
    color:white;
    border-color: white;
    box-shadow: 0 0 10px white;
  }
</style>
