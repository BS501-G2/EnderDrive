<script lang="ts">
  import { FileType, useServerContext, type FileResource } from '$lib/client/client'
  import { useAppContext } from '$lib/client/contexts/app'
  import { useFileBrowserContext, type FileEntry } from '$lib/client/contexts/file-browser'
  import { useFileBrowserListContext } from '$lib/client/contexts/file-browser-list'
  import UserLink from '$lib/client/model/user-link.svelte'
  import Button from '$lib/client/ui/button.svelte'
  import Icon from '$lib/client/ui/icon.svelte'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import { toReadableSize } from '$lib/client/utils'
  import { onMount, type Snippet } from 'svelte'
  import { get, writable } from 'svelte/store'
  import { fly } from 'svelte/transition'
  import moment, * as Moment from 'moment'
  import FileBrowserFileIcon from './file-browser-file-icon.svelte'

  const {
    ...props
  }:
    | {
        file: FileEntry
      }
    | { head: true } = $props()
  const { pushFile, selectedFileIds, selectFile, deselectFile } = useFileBrowserListContext()
  const {
    getFileContents,
    getFileMime,
    me,
    getFileSnapshots,
    getFiles,
    getMainFileContent,
    getUser,
    getFileSize
  } = useServerContext()
  const { isMobile, isDesktop } = useAppContext()
  const { onFileId } = useFileBrowserContext()

  const fileElement = writable<HTMLElement>(null as never)
  const hover = writable<boolean>(false)

  if (!('head' in props)) {
    onMount(() => pushFile(props.file, $fileElement))
  }

  const getModified = async (file: FileEntry) => {
    const fileContent = await getMainFileContent(file.file.id)
    const fileSnapshot = (await getFileSnapshots(file.file.id, fileContent.id, void 0, 0, 1))[0]
    const user = await getUser(fileSnapshot.authorUserId)

    return [fileSnapshot, user!] as const
  }

  const getSize = async (file: FileEntry) => {
    let fileSize = await getFileSize(file.file.id)

    return toReadableSize(fileSize)
  }

  const getFolderSize = async (file: FileEntry) => {
    const self = await me()
    const files = await getFiles(file.file.id, void 0, void 0, self.id, void 0)

    return files.length
  }
</script>

{#snippet size(file: FileEntry)}
  <p class="size">
    {#if file.file.type === FileType.File}
      {#await getSize(file)}
        <LoadingSpinner size="1em" />
      {:then size}
        {size}
      {/await}
    {:else}
      {#await getFolderSize(file) then files}
        {#if files === 0}
          Empty
        {:else if files === 1}
          {files}
          file
        {:else}
          {files}
          files
        {/if}
      {/await}
    {/if}
  </p>
{/snippet}

<!-- svelte-ignore a11y_interactive_supports_focus -->
<!-- svelte-ignore a11y_no_static_element_interactions -->
<div
  bind:this={$fileElement}
  oncontextmenu={(event) => {
    if (get(isMobile)) {
      event.preventDefault()

      if (!('head' in props)) {
        $selectedFileIds.includes(props.file.file.id)
          ? deselectFile(props.file.file.id)
          : selectFile(props.file.file.id)
      }
    }
  }}
  onclick={(event) => {
    if (get(isMobile) && $selectedFileIds.length) {
      event.preventDefault()
      if (!('head' in props)) {
        $selectedFileIds.includes(props.file.file.id)
          ? deselectFile(props.file.file.id)
          : selectFile(props.file.file.id)
      }
    }
  }}
  class="file"
  class:mobile={$isMobile}
  onmouseenter={() => {
    $hover = true
  }}
  onmouseleave={() => {
    $hover = false
  }}
  onkeypress={() => {}}
>
  {#if ($isDesktop || ($isMobile && $selectedFileIds.length)) && !('head' in props)}
    {@const file = props.file}
    <div
      class="check"
      transition:fly={{
        x: -16,
        duration: 150
      }}
    >
      {#if $hover || $selectedFileIds.includes(file.file.id)}
        <button
          class="check"
          onclick={() => {
            $selectedFileIds.includes(file.file.id)
              ? deselectFile(file.file.id)
              : selectFile(file.file.id)
          }}
        >
          {#if $selectedFileIds.includes(file.file.id)}
            <Icon icon="circle-check" size="18px" />
          {:else}
            <Icon icon="circle" size="18px" />
          {/if}
        </button>
      {/if}
    </div>
  {/if}

  <div class="preview">
    {#if !('head' in props)}
      {@const file = props.file}

      {#if file.file.type !== FileType.File}
        <Icon icon="folder" size="32px" />
      {:else}
        {#await getFileMime(file.file.id)}
          <LoadingSpinner size="32px" />
        {:then mime}
          <FileBrowserFileIcon {mime} size="32px" />
        {/await}
      {/if}
    {/if}
  </div>

  <div class="name">
    {#if !('head' in props)}
      {@const file = props.file}
      <div class="file-name">
        <a
          href="/app/files?fileId={file.file.id}"
          ondblclick={(event) => {
            onFileId?.(event as never, file.file.id)
          }}
          onclick={(event) => {
            event.preventDefault()

            if ($isMobile) {
              if ($selectedFileIds.length === 0) {
                onFileId?.(event as never, file.file.id)
              }
            } else {
              $selectedFileIds = [file.file.id]
            }
          }}
          class:mobile={$isMobile}
        >
          <p class="name">
            {file.file.name}
          </p>

          {#if $isMobile}
            {@render size(file)}
          {/if}
        </a>
      </div>
    {:else}
      <h2>File Name</h2>
    {/if}
  </div>

  {#if $isDesktop}
    <div class="size">
      {#if !('head' in props)}
        {@render size(props.file)}
      {:else}
      <h2>File Size</h2>
      {/if}
    </div>
  {/if}

  {#if $isDesktop}
    <div class="modified">
      {#if !('head' in props)}
        {@const file = props.file}
        {#if file.file.type === FileType.File}
          {#await getModified(file)}
            <LoadingSpinner size="1em" />
          {:then [fileSnapshot, user]}
            <p class="user">
              {moment.unix(new Date(fileSnapshot.createTime).getTime() / 1000).fromNow()}
              by
              <UserLink userId={user.id} />
            </p>
          {/await}
        {/if}
      {:else}
        <h2>Modified by</h2>
      {/if}
    </div>

    <div class="date"></div>
  {/if}
</div>

<style lang="scss">
  @use '../../../global.scss' as *;

  div.file {
    color: var(--color-1);

    flex-direction: row;
    align-items: center;

    overflow: hidden;

    padding: 8px;
    gap: 8px;

    h2 {
      font-weight: bolder;
    }

    > div.check {
      @include force-size(32px, 32px);

      align-items: center;
      justify-content: center;

      > button {
        border: none;
        text-decoration: none;

        cursor: pointer;
      }
    }

    > div.preview {
      @include force-size(32px, 32px);
    }

    > div.name {
      flex-grow: 1;
      min-width: 172px;

      flex-direction: row;
      align-items: center;

      > div.file-name {
        flex-direction: column;
        flex-grow: 1;

        min-width: 0;

        > a {
          text-decoration: none;

          color: inherit;

          min-width: 0;

          > p.name {
            text-overflow: ellipsis;
            text-wrap: nowrap;

            overflow: hidden;

            line-height: 1.2em;
          }

          p.size {
            font-size: 0.8em;
          }
        }

        > a.mobile {
          flex-grow: 1;
        }
      }

      > div.actions {
        flex-direction: row;

        div.foreground {
          padding: 8px;
        }
      }
    }

    > div.modified {
      flex-shrink: 0;

      @include force-size(256px, &);
    }

    > div.size {
      @include force-size(96px, &);

      text-align: end;
    }
  }

  div.file.mobile {
    padding: 8px;
  }

  div.file:hover {
    background-color: var(--color-5);

    > div.name {
      > div.file-name {
        > a:not(.mobile) {
          text-decoration: underline;
        }
      }
    }
  }
</style>
