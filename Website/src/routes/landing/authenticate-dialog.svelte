<script lang="ts">
  import { useAppContext } from '$lib/client/contexts/app'
  import Favicon from '$lib/client/ui/favicon.svelte'
  import { derived } from 'svelte/store'
  import AuthenticateDialogLoginForm from './authenticate-dialog-login-form.svelte'
  import RequireClient from '$lib/client/ui/require-client.svelte'
  import { useClientContext } from '$lib/client/client'
  import AuthenticateDialogContinue from './authenticate-dialog-continue.svelte'
  import Window from '$lib/client/ui/window.svelte'
  import { page } from '$app/stores'
  import { goto } from '$app/navigation'

  const { isDesktop, isMobile } = useAppContext()
  const { authentication } = useClientContext()

  const {
    ondismiss
  }: {
    ondismiss: () => void
  } = $props()

  const redirectPath = derived(page, (page) => page.url.searchParams.get('return') ?? null)

  async function redirect() {
    await goto($redirectPath ?? '/app')
  }
</script>

<Window {ondismiss}>
  <div class="form" class:mobile={$isMobile} class:desktop={$isDesktop}>
    <div class="logo">
      <Favicon size={64} />

      <p>EnderDrive</p>
    </div>

    <RequireClient>
        {#if $authentication != null}
          <AuthenticateDialogContinue {redirect} {ondismiss} />
        {:else}
          <AuthenticateDialogLoginForm {redirect} {ondismiss} />
        {/if}
    </RequireClient>
  </div>
</Window>

<style lang="scss">
  @use '../../global.scss' as *;

  div.form {
    flex-grow: 1;

    @include force-size(320px, &);

    justify-content: safe center;

    gap: 16px;

    > div.logo {
      flex-direction: row;
      align-items: center;
      justify-content: center;

      gap: 8px;

      > p {
        font-size: 2rem;
        font-weight: lighter;
      }
    }
  }
</style>
