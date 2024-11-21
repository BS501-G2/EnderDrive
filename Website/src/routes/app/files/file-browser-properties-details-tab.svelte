<script lang="ts">
  import type { Snippet } from 'svelte'
  import FileBrowserPropertiesTab from './file-browser-properties-tab.svelte'
  import { FileType } from '$lib/client/client'
  import type { FileProperties } from '$lib/client/contexts/file-browser'

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
        {#if files[0].file.type === FileType.File}
          {@render field('Type', files[0].mime)}
        {:else if files[0].file.type === FileType.Folder}
          {@render field('Type', 'Folder')}
        {/if}
        {@render field('Created', `${files[0].created}`)}
        {@render field('Modified', `${files[0].modified}`)}
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
