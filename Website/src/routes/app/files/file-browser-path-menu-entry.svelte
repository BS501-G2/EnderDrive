<script lang="ts">
  import { FileType, type FileResource } from '$lib/client/client'
  import Icon from '$lib/client/ui/icon.svelte'
  import Separator from '$lib/client/ui/separator.svelte'
  import FileBrowserPathMenu from './file-browser-path-menu.svelte'

  const {
    file
  }: {
    file: FileResource
  } = $props()

  let button: HTMLButtonElement = $state(null as never)
  let show: boolean = $state(false)
</script>

<div class="list-entry">
  <div class="icon">
    <Icon icon={file.type === FileType.Folder ? 'folder' : 'file'} size="1em" />
  </div>

  <div class="name">
    <a href="/app/files?fileId={file.id}">
      <p class="name" title={file.name}>
        {file.name}
      </p>
    </a>
  </div>

  {#if file.type === FileType.Folder}
    <Separator vertical />

    <button
      bind:this={button}
      class="expand"
      onclick={() => {
        show = true
      }}
    >
      <Icon icon="chevron-right" thickness="solid" size="1em" />
    </button>
  {/if}
</div>

{#if show}
  <FileBrowserPathMenu
    {file}
    {button}
    ondismiss={() => (show = false)}
    cascade
  />
{/if}

<style lang="scss">
  @use '../../../global.scss' as *;

  div.list-entry {
    flex-direction: row;

    line-height: 1em;

    > div.icon {
      @include force-size(1em, &);

      align-items: center;
      justify-content: center;

      padding: 8px;
    }

    > div.name {
      min-width: min(64px);
      flex-grow: 1;

      justify-content: center;

      > a {
        text-decoration: none;
        color: inherit;

        > p.name {
          overflow: hidden hidden;
          text-overflow: ellipsis;
          text-wrap: nowrap;

          min-width: 0;
          max-width: 128px;
        }
      }

      > a:hover {
        text-decoration: underline;
      }
    }

    > button.expand {
      border: none;

      cursor: pointer;
    }

    > button.expand:hover {
      background-color: #0000004f;
    }
  }
</style>
