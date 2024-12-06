<script lang="ts">
  import { useClientContext } from '$lib/client/client'
  import { type UserResource } from '$lib/client/resource'
  import Icon, { type IconOptions } from '$lib/client/ui/icon.svelte'
  import { type Snippet } from 'svelte'
  import Overlay from '../../../overlay.svelte'
  import Button from '$lib/client/ui/button.svelte'
  import { useAppContext } from '$lib/client/contexts/app'
  import { goto } from '$app/navigation'
  import type { Writable } from 'svelte/store'

  const {
    user,
    menuButton,
    ondismiss,
  }: {
    user: UserResource
    menuButton: HTMLButtonElement
    ondismiss: () => void
  } = $props()
  const { isMobile, isDesktop } = useAppContext()

  const x = $derived(
    -(
      1 +
      window.innerWidth -
      menuButton.getBoundingClientRect().x -
      menuButton.getBoundingClientRect().width
    )
  )
  const y = $derived(
    menuButton.getBoundingClientRect().y + menuButton.getBoundingClientRect().height
  )
  const { server } = useClientContext()
</script>

{#snippet menu(x?: number, y?: number, dim: boolean = false)}
  <Overlay {ondismiss} {x} {y} nodim={!dim}>
    <div class="menu">
      {#snippet button(name: string, icon: IconOptions, onclick: () => void)}
        {#snippet foreground(view: Snippet)}
          <div class="foreground">
            {@render view()}
          </div>
        {/snippet}

        <Button {onclick} {foreground}>
          <div class="button">
            <Icon {...icon} />
            <p class="label">{name}</p>
          </div>
        </Button>
      {/snippet}

      {@render button('Go To Profile', { icon: 'person', thickness: 'solid' }, () => {
        goto(`/app/profile?id=${user.Id}`)
        ondismiss()
      })}
      

      {#await server.GetRootId({ UserId: user.Id }) then rootId}
        {#if rootId != null}
          {@render button('View Files', { icon: 'file', thickness: 'solid' }, async () => {
            await goto(`/app/files?fileId=${rootId}`)
          })}
        {/if}
      {/await}
    </div>
  </Overlay>
{/snippet}

{#if $isMobile}
  {@render menu(void 0, void 0, true)}
{:else if $isDesktop}
  {@render menu(x, y)}
{/if}

<style lang="scss">
  div.menu {
    background-color: var(--color-9);
    color: var(--color-1);

    div.foreground {
      flex-grow: 1;
      padding: 8px;

      div.button {
        gap: 8px;

        flex-direction: row;
        align-items: center;
      }
    }
  }
</style>
