<script lang="ts">
  import { writable } from 'svelte/store'
  import type { FileAccessEntry } from './file-browser-properties-access-tab.svelte'
  import Icon, { type IconOptions } from '$lib/client/ui/icon.svelte'
  import UserLink from '$lib/client/model/user-link.svelte'
  import { type Snippet } from 'svelte'
  import Button from '$lib/client/ui/button.svelte'
  import { FileAccessLevel, type UserResource } from '$lib/client/resource'

  const {
    access,
    onedit
  }: { access: FileAccessEntry; onedit: (user: UserResource | null, preset: FileAccessLevel | null) => void | Promise<void> } =
    $props()

  const hover = writable(false)
  const userElement = writable<HTMLElement>(null as never)

  $effect(() => {
    if ($userElement) {
      const onmouseenter = () => {
        hover.set(true)
      }

      const onmouseleave = () => {
        hover.set(false)
      }

      $userElement.addEventListener('mouseenter', onmouseenter)
      $userElement.addEventListener('mouseleave', onmouseleave)

      return () => {
        $userElement.removeEventListener('mouseenter', onmouseenter)
        $userElement.removeEventListener('mouseleave', onmouseleave)
      }
    }
  })
</script>

<div class="user" bind:this={$userElement}>
  <div class="icon">
    {#if access.user == null}
      <Icon icon="globe" thickness="solid" size="2rem" />
    {:else}
      <Icon icon="user-circle" size="2rem" />
    {/if}
  </div>

  <div class="info">
    <p class="name">
      {#if access.user != null}
        <UserLink userId={access.user.Id} />
      {:else}
        Public Access
      {/if}
    </p>
    <p class="level">
      Has <span class="emphasis">{FileAccessLevel[access.access.Level]}</span> accsess
    </p>
  </div>

  {#if $hover}
    <div class="actions">
      {#snippet button(name: string, icon: IconOptions, onclick: () => Promise<void> | void)}
        {#snippet foreground(view: Snippet)}
          <div class="action-foreground">
            {@render view()}
          </div>
        {/snippet}

        <Button {foreground} {onclick} hint={name}>
          <div class="action">
            <Icon {...icon} />
          </div>
        </Button>
      {/snippet}

      {@render button('Edit', { icon: 'pencil', thickness: 'solid' }, () => onedit(access.user ?? null, access.access.Level))}
    </div>
  {/if}
</div>

<style lang="scss">
  div.user {
    flex-direction: row;

    gap: 8px;

    > div.info {
      flex-grow: 1;

      > p.name {
        font-weight: bolder;
      }

      > p.level {
        font-size: 0.8rem;
        font-style: italic;

        > span.emphasis {
          font-weight: bolder;
        }
      }
    }

      > div.actions {
        flex-direction: row;

        div.action-foreground {
          flex-grow: 1;

          padding: 8px;

          div.action {
            align-items: center;
            justify-content: center;
          }
        }
      }
  }
</style>
