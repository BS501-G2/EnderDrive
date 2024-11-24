<script lang="ts">
  import { type NewsResource, useServerContext } from '$lib/client/client'
  import Window from '$lib/client/ui/window.svelte'
  import { Buffer } from 'buffer'

  import { onMount } from 'svelte'
  import { persisted } from 'svelte-persisted-store'
  import { tweened } from 'svelte/motion'
  import { writable } from 'svelte/store'
  const {}: {} = $props()

  const server = useServerContext()

  const news = writable<NewsResource[]>([])
  const newsIndex = tweened(0)

  const readNewsId = persisted<string | null>('read-news-id', null)

  onMount(() => {
    void (async () => {
      if (await server.didIAgree()) {
        const newsIds = await server.getNews($readNewsId ?? void 0)

        $news = await Promise.all(newsIds.map((newsId) => server.getNewsEntry(newsId)))
      }
    })()
  })

  onMount(() => {
    const interval = setInterval(() => {
      $newsIndex = ($newsIndex + 1) % $news.length
    }, 5000)

    return () => clearInterval(interval)
  })

  onMount(() =>
    newsIndex.subscribe((scroll) => {
      if ($element == null) {
        return
      }

      $element.scrollLeft = scroll * $element.scrollWidth
    })
  )

  const element = writable<HTMLDivElement>(null as never)
</script>

{#if $news.length !== 0}
  <Window
    ondismiss={() => {
      const last = $news.at(-1)

      if (last != null) {
        $readNewsId = last.id
      }

      $news = []
    }}
    title="News"
    titleIcon={{ icon: 'newspaper' }}
  >
    <div class="news" bind:this={$element}>
      {#each $news as { title, image }}
        <div class="entry">
          <img
            src={URL.createObjectURL(new Blob([Buffer.from(`${image}`, 'base64')]))}
            alt="news"
          />
          <p>{title}</p>
        </div>
      {/each}
    </div>
  </Window>
{/if}

<style lang="scss">
  @use '../../global.scss' as *;

  div.news {
    @include force-size(min(75dvw, 640px), min(75dvh, 360px));

    flex-direction: row;

    overflow: auto hidden;

    > div.entry {
      @include force-size(100%, 100%);

      > img {
        @include force-size(&, 100%);

        object-fit: contain;
      }
    }
  }
</style>
