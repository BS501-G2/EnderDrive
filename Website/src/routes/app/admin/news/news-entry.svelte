<script lang="ts">
  import { type NewsResource } from '$lib/client/resource'
  import { Buffer } from 'buffer'

  const { news }: { news: NewsResource } = $props()
  const { Image, Title } = news

  const url = URL.createObjectURL(new Blob([Buffer.from(`${Image}`, 'base64')]))
</script>

<div class="entry">
  <div class="image">
    <img alt="news-banner" src={url} />
  </div>

  <div class="title">
    <p>{Title}</p>
  </div>

  <div class="actions"></div>
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
