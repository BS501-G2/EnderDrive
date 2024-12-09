<script lang="ts">
  import { useClientContext } from '$lib/client/client'
  import { type NewsResource } from '$lib/client/resource'
  import Button from '$lib/client/ui/button.svelte'
  import Icon, { type IconOptions } from '$lib/client/ui/icon.svelte'
  import { Buffer } from 'buffer'
  import { type Snippet } from 'svelte'

  const { server } = useClientContext()
  const { ...props }: { header: true } | { news: NewsResource; onrefresh: () => void } = $props()
</script>

<div class="entry">
  <div class="image">
    {#if 'header' in props}
      <b class="title">Banner</b>
    {:else}
      <img
        alt="news-banner"
        src={URL.createObjectURL(new Blob([Buffer.from(`${props.news.Image}`, 'base64')]))}
      />
    {/if}
  </div>

  <div class="title">
    {#if 'header' in props}
      <b class="title">Title</b>
    {:else}
      <p>{props.news.Title}</p>
    {/if}
  </div>

  {#if !('header' in props)}
    <div class="actions">
      {#snippet button(icon: IconOptions, name: string, onclick: () => Promise<void>)}
        {#snippet foreground(view: Snippet)}
          <div class="foreground">
            {@render view()}
          </div>
        {/snippet}

        <Button {foreground} hint={name} {onclick}>
          <Icon {...icon} />
        </Button>
      {/snippet}

      {@render button({ icon: 'trash-can' }, 'Delete News', async () => {
        await server.DeleteNews({ NewsId: props.news.Id })
        await props.onrefresh()
      })}
    </div>
  {/if}
</div>

<style lang="scss">
  @use '../../../../global.scss' as *;

  b.title {
    font-weight: bolder;
  }

  div.entry {
    flex-direction: row;

    align-items: center;

    gap: 8px;
    padding: 8px;

    > div.image {
      @include force-size(64px, &);

      > img {
        @include force-size(&, 32px);

        object-fit: contain;
      }
    }

    > div.title {
      flex-grow: 1;
    }

    > div.actions {
      div.foreground {
        padding: 8px;
      }
    }
  }

  div.entry:hover {
    background-color: var(--color-5);
  }
</style>
