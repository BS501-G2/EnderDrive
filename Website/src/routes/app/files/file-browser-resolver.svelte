<script lang="ts">
  import { FileType, TrashOptions, useServerContext } from '$lib/client/client'
  import {
    FileBrowserResolveType,
    useFileBrowserContext,
    type CurrentFile,
    type FileBrowserAction,
    type FileBrowserResolve,
    type FileEntry
  } from '$lib/client/contexts/file-browser'
  import { get, type Readable, type Writable } from 'svelte/store'
  import { onMount } from 'svelte'
  import Banner from '$lib/client/ui/banner.svelte'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import FileBrowserFileList from './file-browser-file-list.svelte'
  import FileBrowserFileView from './file-browser-file-view.svelte'

  const {
    resolve,
    current,
    actions
  }: {
    resolve: Readable<FileBrowserResolve>
    current: Writable<CurrentFile>
    actions: Readable<FileBrowserAction[]>
  } = $props()
  const { getFiles, getFileAccesses, getFilePath, getFileStars, getFile, getFileMime, me } =
    useServerContext()
  const { pushRefresh } = useFileBrowserContext()

  async function load(
    resolve: FileBrowserResolve,
    offset?: number,
    length?: number
  ): Promise<void> {
    const load = async (): Promise<CurrentFile> => {
      const self = await me()

      switch (resolve[0]) {
        case FileBrowserResolveType.File: {
          const [, fileId] = resolve

          const file = await getFile(fileId ?? void 0)
          const path = await getFilePath(file.id)

          if (file.type === FileType.File) {
            return {
              type: 'file',
              mime: 'application/octet-stream',
              path,
              file
            }
          } else if (file.type === FileType.Folder) {
            const files = await getFiles(
              file.id,
              void 0,
              void 0,
              self.id,
              undefined,
              offset,
              length
            )

            return {
              type: 'folder',
              path,
              files: await Promise.all(
                files.map(async (file): Promise<FileEntry> => {
                  if (file.type === FileType.File) {
                    return {
                      type: 'file',
                      file
                    } as FileEntry
                  } else if (file.type === FileType.Folder) {
                    return {
                      type: 'folder',
                      file
                    } as FileEntry
                  } else {
                    throw new Error('Invalid file type.')
                  }
                })
              ),
              file
            }
          } else {
            throw new Error('Invalid file type.')
          }
        }

        case FileBrowserResolveType.Shared: {
          const fileAccesses = await getFileAccesses(
            undefined,
            undefined,
            undefined,
            undefined,
            undefined,
            offset,
            length
          )
          return {
            type: 'shared',
            files: await Promise.all(
              fileAccesses.map(async (fileAccess): Promise<FileEntry> => {
                const file = await getFile(fileAccess.fileId)

                return {
                  type: 'shared',
                  file,
                  fileAccess
                }
              })
            )
          }
        }

        case FileBrowserResolveType.Starred: {
          const fileStars = await getFileStars(undefined, undefined, offset, length)

          return {
            type: 'starred',
            files: await Promise.all(
              fileStars.map(async (fileStar): Promise<FileEntry> => {
                const file = await getFile(fileStar.fileId)

                return {
                  type: 'starred',
                  file,
                  fileStar
                }
              })
            )
          }
        }

        case FileBrowserResolveType.Trash: {
          const files = await getFiles(
            void 0,
            void 0,
            void 0,
            self.id,
            TrashOptions.Exclusive,
            offset,
            length
          )

          return {
            type: 'trash',
            files: await Promise.all(
              files.map(async (file) => {
                return {
                  type: 'trash',
                  file
                }
              })
            )
          }
        }
      }
    }

    try {
      current.set({
        type: 'loading'
      })
      const result = await load()

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
  <Banner
    type="error"
    icon={{
      icon: 'xmark',
      thickness: 'solid',
      size: '1.5em'
    }}
  >
    {$current.error.message}
  </Banner>
{:else if $current.type === 'loading'}
  <div class="loading">
    <LoadingSpinner size="3em" />
  </div>
{:else if $current.type === 'folder' || $current.type === 'shared' || $current.type === 'starred' || $current.type === 'trash'}
  <FileBrowserFileList current={$current} />
{:else if $current.type === 'file'}
  <FileBrowserFileView file={$current.file} {actions} />
{/if}

<style lang="scss">
  @use '../../../global.scss' as *;

  div.loading {
    flex-grow: 1;

    align-items: center;
    justify-content: center;

    @include force-size(100%, 100%);
  }
</style>
