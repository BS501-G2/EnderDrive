<script lang="ts">
  import type { NewsResource } from '$lib/client/client'
  import { Buffer } from 'buffer'

  const { news }: { news: NewsResource } = $props()
  const { image, title } = news

  const url = URL.createObjectURL(new Blob([Buffer.from(`${image}`, 'base64')]))
</script>

<div class="entry">
  <div class="image">
    <img alt="news-banner" src={url} />
  </div>

  <div class="title">
    <p>{title}</p>
  </div>

  <div class="actions">
    
  </div>
</div>

<style lang="scss">
  @use '../../../../global.scss' as *;

  div.entry {
    flex-direction: row;

    align-items: center;

    > div.image {
      > img {
        @include force-size(64px, 64px);

        object-fit: contain;
      }
    }

    > div.title {
      flex-grow: 1;
    }
  }
</style>
