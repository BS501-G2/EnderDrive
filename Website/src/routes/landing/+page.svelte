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
  <div class="firstGrid-Container" id="home">
    <div class="gridInfo1">
      <h1>EnderDrive a Centralized Filesystem for Melchora Aquino Elementary School.</h1>
      <p>Secure and Private File Storage and Sharing Website for Melchora Aquino Elementary School.</p>

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

<LandingEntry name="Features" hideHeader>
  <div class="firstGrid-Container">
    <div class="gridVid">
      <video src="SecurityWhiteMG.mp4" muted loop autoplay playsinline></video>
    </div>
     <div class="gridInfo1">
      <h1>Secure File Storage and Sharing</h1>
      <p>Keep your files safe with advanced encryption and secure access controls. EnderDrive is built to protect sensitive information, ensuring peace of mind for users. Designed specifically for the needs of Melchora Aquino Elementary School.</p>
    </div>
  </div>
  <div class="firstGrid-Container">
     <div class="gridInfo1">
      <h1>Effortless Collaboration</h1>
      <p>Work seamlessly with your team by sharing and managing files across devices. EnderDrive makes real-time collaboration simple and efficient, no matter where you are. Enjoy a hassle-free experience tailored for education and teamwork.</p>
    </div>
    <div class="gridVid">
      <video src="CollaborationMG.mp4" muted loop autoplay playsinline></video>
    </div>
  </div>
  <div class="firstGrid-Container">
    <div class="gridVid">
      <video src="AdminWhiteMG.mp4" muted loop autoplay playsinline></video>
    </div>
     <div class="gridInfo1">
      <h1>Admin Control and Oversight</h1>
      <p>Empower administrators with tools to monitor activity and manage user access effortlessly. EnderDrive ensures that system integrity is maintained with comprehensive oversight. Take full control while providing a secure and user-friendly experience for all.</p>
    </div>

  </div>
  </LandingEntry>

<LandingEntry name="Contact" hideHeader>
<div class="Contact" id="contact">
     <div class="contactDetails">
      <h1>Contact <span class="highlight">Us</span></h1>
      <p>Have any questions? We'd love to hear from you.</p>
    </div>
     <div class="contactGrid-Container">
      <div class="Card">
        <i class="fas fa-envelope"></i>
        <h3>Email</h3>
        <p>melchoraaquinoes.manila@deped.gov.ph</p>
      </div>
      <div class="Card">
        <i class="fas fa-phone"></i>
        <h3>Phone</h3>
        <p>02-2534445</p>
      </div>
      <div class="Card">
        <i class="fas fa-map-marker-alt"></i>
        <h3>Office</h3>
        <p>Solis Street, Barangay 204, Tondo, Manila City, 1012, Metro Manila, Philippines</p>
      </div>
    </div>
  </div>
</LandingEntry>
<LandingEntry name="About" hideHeader>
  <h1>Meet <span class="highlight">The Team</span></h1>
</LandingEntry>
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
    grid-template-columns: 1fr 1fr;
    gap: 40px;
    padding: 50px;
    max-width: 1280px;
    margin: 0 auto;
    padding: 10 20px;
  }
  div.gridInfo1{
    max-width: 100%;
    max-height: 100%;
    overflow: hidden;
    margin: auto;
    text-align: left;
    align-self: center;
  }
  div.gridImage1{
    max-width: 100%;
    max-height: 100%;
    object-fit: contain;
    overflow: hidden;
    align-self: center;
  }
  img{
    max-width: 100%;
    height: auto;
  }
  p{
    text-align: left;
    line-height: 1;
    font-weight: lighter;
    font-size: 25px;
    margin:10px;
  }
  h1{
    font-size: 45px;
    word-spacing: normal;
    letter-spacing: normal;
    text-align: left;
    font-weight: bold;
    line-height: 1;
  }
  div.gridVid video {
    max-width: 100%;
    min-height: 100%;
    object-fit: cover;
    border-radius: inherit;
    overflow: hidden;
  }
  div.gridVid1 video {
    max-width: 100%;
    min-height: 600px;
    object-fit: cover;
    border-radius: inherit;
    overflow: hidden;
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
  div.contactDetails{
    text-align:center;
    padding:30px;
  }
  div.Contact {
    display: flex;
    flex-direction: column;
    align-items: center;
    width: 100%;
    min-height:900px;
  }
  .contactDetails h1 {
    font-size: 4em;
    margin: 0;
    padding:70px;
    text-align: center;
  }
   .contactDetails p {
    font-size: 2em;
    margin: 0;
    padding:10px;
    justify-content: center;
    align-items: center;
  }

  .contactDetails .highlight {
    color: #86c24fff;
  }
  div.contactGrid-Container {
    display: grid;
    place-items: center;
    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
    gap: 10%;
    width: 80%;
    max-width: 1000px;
    padding: 20px;
    background-color: #fff;
    border-radius: 8px;
    padding:70px;
  }
  div.Card {
    background-color: rgba(255, 255, 255, 0.8);
    align-items: center;
    color: black;
    padding: 10px;
    border-radius: 8px;
    text-align: center;
    box-shadow: 0 4px 6px rgba(0, 0, 0, 0.25);
    transition: transform 0.3s, background-color 0.3s;
    height: auto;
    max-width:100%;
    width: 100%;
  }
  .Card i {
    font-size: 2em;
    margin-bottom: 10px;
    color: #86c24fff;
    transition: color 0.3s;
  }

  .Card:hover i {
    color: white;

  }

  div.Card:hover {
    background-color: #86c24fff;
    color: white;
    transform: translateY(-5px);
  }

  div.Card h3 {
    margin: 0 0 10px;
    font-size: medium;
    padding:10px;
    text-align: center;
  }

  div.Card p {
    margin: 0;
    font-size: medium;
    padding: 20px;
    text-align: center;
    word-wrap: break-word;
    word-break: break-word;
    white-space: normal;
    overflow: visible;
    min-height: 50px;
  }

  @media (max-width:1024px) {
    div.firstGrid-Container{
        grid-template-columns: auto auto;
        padding:30px;
        align-items: center;
      }
    h1{
      font-size: 35px;
      text-align: center;
      line-height: 1;
    }
    p{
      font-size: 20px;
      text-align: center;
    }
    div.button-container1{
      justify-content: center;
    }
    div.gridImage1{
      max-width: 80%;
    }
    div.button-container1{
        flex-direction: column;
        align-items: center;
        gap:10px;
      }
    .btn{
        width: 100%;
        padding:10px 10px;
        font-size: large;
        max-width: 200px;
        text-align: center;
      }
      div.contactDetails {
      padding: 20px;
     }
     .contactDetails h1 {
    font-size: 3em;
    padding: 50px;
  }

  .contactDetails p {
    font-size: 1.5em;
    padding: 10px;
  }

  div.contactGrid-Container {
    gap: 40px;
    padding: 40px;
  }
  div.Card {
      padding: 20px;
      max-width: 90%;
    }
  div.Card p{
    font-size:small;
  }
   div.Card h3{
    font-size:medium;
  }

  }
  @media (max-width:768px) {
      div.firstGrid-Container{
        grid-template-columns: 1fr;
        padding:30px;
        align-items: center;
      }
      h1{
        font-size: 35px;
        text-align: center;
      }
      p{
        font-size: 20px;
        line-height: 1.5;
        text-align: center;
      }
      div.gridImage1 {
      display: none;
      }
      div.gridVid{
        order: 1;
      }
      div.gridInfo1{
        order: 2;
      }
      div.button-container1{
        justify-content: center;
        grid-template-columns: auto auto;
        margin-top: 15px;
      }
      .btn{
        width: 100%;
        padding:8px 10px;
        font-size: medium;
        max-width: 200px;
        text-align: center;
      }
      div.Contact {
      min-height: auto;
      }
    .contactDetails h1 {
      font-size: 2.5em;
      padding: 30px;
    }

    .contactDetails p {
      font-size: 1.3em;
    }

    div.contactGrid-Container{
        display: grid;
        place-items:center;
        grid-template-columns: 1fr 1fr 1fr;
      }

    div.Card {
      padding: 10px;
      max-width: 100%;
    }

    div.Card h3 {
      font-size: small;
    }

    div.Card p {
      font-size: x-small;
    }
    }
    @media (max-width: 480px) {
      div.firstGrid-Container{
        grid-template-columns: 1fr;
        padding:20px;
        gap:10px;
        align-items: center;
      }
      h1{
        font-size: 25px;
        text-align: center;
        line-height: 1;
      }
      p{
        font-size: 15px;
        text-align:center;
        line-height: 1;
      }
      div.button-container1{
        flex-direction: column;
        align-items: center;
        gap:10px;
      }
      .btn{
        width: 100%;
        padding:6px 7px;
        font-size: medium;
        max-width: 200px;
        text-align: center;
      }
      div.contactGrid-Container{
        display: grid;
        place-items:center;
        grid-template-columns: 1fr 1fr 1fr;
      }
      div.Card {
      padding: 5px;
      max-width: 100%;
    }
      div.Card h3 {
      font-size: xx-small;
    }

    div.Card p {
      font-size:xx-small;
    }
  }
</style>
