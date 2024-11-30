<script lang="ts">
  import type { Snippet } from 'svelte'
  import FileBrowserPropertiesTab from './file-browser-properties-tab.svelte'
  import type { FileProperties } from '$lib/client/contexts/file-browser'
  import UserLink from '$lib/client/model/user-link.svelte'
  import { FileType } from '$lib/client/resource'

  const { files }: { files: FileProperties[] } = $props()
</script>

<FileBrowserPropertiesTab label="Details" icon={{ icon: 'info', thickness: 'solid' }}>
  <div class="overview">
    {#snippet field(name: string, value: Snippet | string)}
      <div class="row">
        <p class="label">
          {name}
        </p>
        <p class="value">
          {#if typeof value === 'string'}
            {value}
          {:else}
            {@render value()}
          {/if}
        </p>
      </div>
    {/snippet}

    {#if files.length === 1}
      {#if files[0].type === 'file'}
        {@render field('Type', files[0].mime)}
      {:else if files[0].file.Type === FileType.Folder}
        {@render field('Type', 'Folder')}
      {/if}
      {@render field('Created', `${files[0].created}`)}
      {@render field('Modified', `${files[0].modified}`)}

      {#snippet user()}
        <UserLink userId={files[0].file.OwnerUserId!} />
      {/snippet}

      {@render field('Owner', user)}
    {/if}
  </div>
</FileBrowserPropertiesTab>

<style lang="scss">
  @use '../../../global.scss' as *;

  div.overview {
    flex-grow: 1;

    padding: 8px;
    gap: 8px;

    > div.row {
      flex-direction: row;

      gap: 4px;

      > p.label {
        @include force-size(96px, &);

        text-align: end;
        font-weight: bolder;
      }

      > p.label::after {
        content: ':';
      }

      > p.value {
        flex-grow: 1;
      }
    }
  }
</style>
