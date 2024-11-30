<script lang="ts">
  import { useClientContext } from '$lib/client/client'
  import {
    FileBrowserResolveType,
    useFileBrowserContext,
    type CurrentFile,
    type FileBrowserAction,
    type FileBrowserResolve,
    type FileEntry
  } from '$lib/client/contexts/file-browser'
  import { get, writable, type Readable, type Writable } from 'svelte/store'
  import { onMount, type Snippet } from 'svelte'
  import Banner from '$lib/client/ui/banner.svelte'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import FileBrowserFileList from './file-browser-file-list.svelte'
  import FileBrowserFileView from './file-browser-file-view.svelte'
  import Button from '$lib/client/ui/button.svelte'
  import { FileType, TrashOptions } from '$lib/client/resource'

  const {
    resolve,
    current,
    actions
  }: {
    resolve: Readable<FileBrowserResolve>
    current: Writable<CurrentFile>
    actions: Readable<FileBrowserAction[]>
  } = $props()
  const { server } = useClientContext()
  const { pushRefresh } = useFileBrowserContext()

  const displayedFiles = writable<FileEntry[]>([])
  const lastCurrentFile: Writable<CurrentFile | null> = writable(null)

  async function load(resolve: FileBrowserResolve, offset?: number, count?: number): Promise<void> {
    const load = async (): Promise<CurrentFile> => {
      const me = await server.Me({})

      switch (resolve[0]) {
        case FileBrowserResolveType.File: {
          const [, fileId] = resolve
          const targetId = (fileId ?? (await server.GetRootId({ UserId: me.Id })))!
          const fileData = await server
            .FileGetDataEntries({ FileId: targetId })
            .then((result) => result[0])

          const file = await server.GetFile({ FileId: targetId })
          const path = await server.GetFilePath({ FileId: targetId })

          if (file.Type === FileType.File) {
            return {
              type: 'file',
              mime: await server.FileGetMime({
                FileId: targetId,
                FileDataId: fileData.Id
              }),
              path,
              file,
              me: me
            }
          } else if (file.Type === FileType.Folder) {
            const files = await server.GetFiles({
              // file.id, void 0, void 0, void 0, undefined, offset, count
            })

            return {
              type: 'folder',
              path,
              files: await Promise.all(
                files.map(async (file): Promise<FileEntry> => {
                  if (file.Type === FileType.File) {
                    return {
                      type: 'file',
                      file
                    } as FileEntry
                  } else if (file.Type === FileType.Folder) {
                    return {
                      type: 'folder',
                      file
                    } as FileEntry
                  } else {
                    throw new Error('Invalid file type.')
                  }
                })
              ),
              file,
              me: me
            }
          } else {
            throw new Error('Invalid file type.')
          }
        }

        case FileBrowserResolveType.Shared: {
          const fileAccesses = await server.GetFileAccesses({
            TargetUserId: me.Id,
            IncludePublic: true,
            Pagination: {
              Offset: offset,
              Count: count
            }
          })

          return {
            type: 'shared',
            files: await Promise.all(
              fileAccesses.map(async (fileAccess): Promise<FileEntry> => {
                const file = await server.GetFile({ FileId: fileAccess.FileId })
                return {
                  type: 'shared',
                  file,
                  fileAccess
                }
              })
            ),
            me: me
          }
        }

        case FileBrowserResolveType.Starred: {
          const fileStars = await server.GetFileStars({
            UserId: me.Id,
            Pagination: {
              Offset: offset,
              Count: count
            }
          })

          return {
            type: 'starred',
            files: await Promise.all(
              fileStars.map(async (fileStar): Promise<FileEntry> => {
                const file = await server.GetFile({ FileId: fileStar.FileId })

                return {
                  type: 'starred',
                  file,
                  fileStar
                }
              })
            ),
            me: me
          }
        }

        case FileBrowserResolveType.Trash: {
          const files = await server.GetFiles({
            TrashOptions: TrashOptions.Exclusive,
            OwnerUserId: me.Id,
            Pagination: {
              Offset: offset,
              Count: count
            }
          })

          return {
            type: 'trash',
            files: await Promise.all(
              files.map(async (file) => {
                return {
                  type: 'trash',
                  file
                }
              })
            ),
            me: me
          }
        }
      }
    }

    try {
      current.set({
        type: 'loading'
      })
      const result = await load()

      if (
        result.type === 'shared' ||
        result.type === 'folder' ||
        result.type === 'starred' ||
        result.type === 'trash'
      ) {
        displayedFiles.update((files) => {
          files.push(...result.files)
          return files
        })
      }

      lastCurrentFile.set(result)
      current.set(result)
    } catch (error: any) {
      console.error(error)
      current.set({
        type: 'error',
        error
      })
    }
  }

  onMount(() => resolve.subscribe(load))

  onMount(() =>
    pushRefresh(() => {
      load(get(resolve))
    })
  )
</script>

{#if $current.type === 'error'}
  <div class="banner">
    <Banner
      type="error"
      icon={{
        icon: 'xmark',
        thickness: 'solid',
        size: '1.5em'
      }}
    >
      <p>
        {$current.error.message}
      </p>

      {#snippet bottom()}
        {#snippet retryForeground(view: Snippet)}
          <div class="retry-foreground">
            {@render view()}
          </div>
        {/snippet}

        <Button
          foreground={retryForeground}
          onclick={async () => {
            await load($resolve)
          }}
        >
          Retry
        </Button>
      {/snippet}
    </Banner>
  </div>
{:else if $current.type === 'loading'}
  <div class="loading">
    <LoadingSpinner size="3em" />
  </div>
{:else if $current.type === 'folder' || $current.type === 'shared' || $current.type === 'starred' || $current.type === 'trash'}
  <FileBrowserFileList displayedFiles={$displayedFiles} current={$current} />
{:else if $current.type === 'file'}
  <FileBrowserFileView file={$current.file} />
{/if}

<style lang="scss">
  @use '../../../global.scss' as *;

  div.banner {
    flex-grow: 1;

    align-items: center;

    justify-content: center;

    div.retry-foreground {
      padding: 8px;
    }
  }

  div.loading {
    flex-grow: 1;

    align-items: center;
    justify-content: center;

    @include force-size(100%, 100%);
  }
</style>
