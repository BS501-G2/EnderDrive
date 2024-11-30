<script lang="ts">
  import { type FileAccessResource } from '$lib/client/resource';
  import UserLink from '$lib/client/model/user-link.svelte'
  import Icon from '$lib/client/ui/icon.svelte'
  import SharedEntry from './shared-entry.svelte'

  const { userId, fileAccesses }: { userId: string; fileAccesses: FileAccessResource[] } = $props()
</script>

<div class="group">
  <div class="icon">
    <Icon icon="user-circle" size="2rem" />
  </div>

  <div class="main">
    <div class="user">
      <p><UserLink {userId} /> has shared {fileAccesses.length} file(s) to you.</p>
    </div>

    <div class="carousel">
      {#each fileAccesses as file}
        <SharedEntry fileAccess={file} />
      {/each}
    </div>
  </div>
</div>

<style lang="scss">
  @use '../../../global.scss' as *;
  div.group {
    background-color: var(--color-5);
    color: var(--color-1);

    flex-direction: row;

    gap: 8px;
    padding: 16px;

    border-radius: 8px;

    > div.main {
      gap: 8px;

      > div.user {
        flex-direction: row;
        gap: 8px;

        > p {
          text-wrap: nowrap;
        }
      }

      > div.carousel {
        flex-direction: row;

        gap: 16px;
      }
    }
  }
</style>
