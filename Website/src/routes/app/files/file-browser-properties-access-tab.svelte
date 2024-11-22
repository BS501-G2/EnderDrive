<script lang="ts" module>
  export interface FileAccessEntry {
    access: FileAccessResource
    user: UserResource | null
  }
</script>

<script lang="ts">
  import type { FileProperties } from '$lib/client/contexts/file-browser'
  import { writable, type Writable } from 'svelte/store'
  import FileBrowserPropertiesTab from './file-browser-properties-tab.svelte'
  import {
    FileAccessLevel,
    useServerContext,
    type FileAccessResource,
    type FileResource,
    type UserResource
  } from '$lib/client/client'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import Button from '$lib/client/ui/button.svelte'
  import { type Snippet } from 'svelte'
  import Icon, { type IconOptions } from '$lib/client/ui/icon.svelte'
  import FileBrowserPropertiesAccessTabAdd from './file-browser-properties-access-tab-add.svelte'
  import FileBrowserPropertiesAccessTabEdit from './file-browser-properties-access-tab-edit.svelte'
  import Overlay from '../../overlay.svelte'
  import FileBrowserPropertiesAccessTabUserEntry from './file-browser-properties-access-tab-user-entry.svelte'


  const { file }: { file: FileProperties } = $props()
  const { getFileAccesses, getUser, me } = useServerContext()

  async function load(): Promise<FileAccessEntry[]> {
    const fileAccesses = await getFileAccesses({
      targetFileId: file.file.id,
      level: FileAccessLevel.Read,
      includePublic: true
    })

    return await Promise.all(
      fileAccesses.map(async (access): Promise<FileAccessEntry> => {
        const user = access.targetUserId != null ? await getUser(access.targetUserId) : null

        return {
          access,
          user
        }
      })
    )
  }

  const addButton: Writable<HTMLButtonElement> = writable(null as never)

  const promise: Writable<Promise<FileAccessEntry[]>> = writable(load())
  const showAddMenu = writable(false)
  const addDialog = writable<boolean>(false)
  const showEditDialog = writable<boolean>(false)
  const editDialog = writable<
    [user: UserResource | null, file: FileResource, preset: FileAccessLevel | null] | null
  >(null)
</script>

{#if $addDialog}
  <FileBrowserPropertiesAccessTabAdd
    ondismiss={() => {
      $addDialog = false
    }}
    onresult={(user) => {
      $editDialog = [user, file.file, null]
      $showEditDialog = true
      $addDialog = false
    }}
  />
{/if}

{#if $showEditDialog && $editDialog != null}
  {console.log($editDialog)}
  {@const [user, file, preset] = $editDialog}

  <FileBrowserPropertiesAccessTabEdit
    {user}
    {file}
    ondismiss={() => {
      $showEditDialog = false
    }}
    refresh={() => {
      $promise = load()
    }}
    existingValue={preset ?? undefined}
  />
{/if}

<FileBrowserPropertiesTab label="Access" icon={{ icon: 'lock', thickness: 'solid' }}>
  <div class="accesses">
    <div class="header">
      <h2 class="title">Users who have access</h2>
      <div class="actions">
        {#snippet foreground(view: Snippet)}
          <div class="foreground">
            {@render view()}
          </div>
        {/snippet}

        <Button
          {foreground}
          onclick={async () => {
            $showAddMenu = true
          }}
          bind:buttonElement={$addButton}
        >
          <Icon icon="add" thickness="solid" />
        </Button>
      </div>
    </div>
    <div class="access-list">
      {#await $promise}
        <LoadingSpinner size="3em" />
      {:then accesses}
        {@const publicAccess = accesses.find(({ access }) => access.targetUserId == null)}
        {#if publicAccess != null}
          <FileBrowserPropertiesAccessTabUserEntry
            access={publicAccess}
            onedit={() => {
              $editDialog = [null, file.file, publicAccess.access.level]
              $showEditDialog = true
            }}
          />
        {/if}

        {#each accesses.filter(({ access }) => access.targetUserId != null) as access}
          <FileBrowserPropertiesAccessTabUserEntry
            {access}
            onedit={() => {
              $editDialog = [access.user, file.file, access.access.level]
              $showEditDialog = true
            }}
          />
        {/each}
      {/await}
    </div>
  </div>
</FileBrowserPropertiesTab>

{#if $showAddMenu}
  <Overlay
    ondismiss={() => {
      $showAddMenu = false
    }}
    x={-8}
    y={$addButton.getBoundingClientRect().y + $addButton.getBoundingClientRect().height}
    nodim
  >
    <div class="add-menu">
      {#snippet button(name: string, icon: IconOptions, onclick: () => void)}
        {#snippet foreground(view: Snippet)}
          <div class="add-menu-foreground">
            {@render view()}
          </div>
        {/snippet}

        <Button {foreground} {onclick}>
          <div class="button">
            <Icon {...icon} />
            <p>{name}</p>
          </div>
        </Button>
      {/snippet}

      {@render button('Grant Access to User', { icon: 'user-circle', thickness: 'solid' }, () => {
        $showAddMenu = false
        $addDialog = true
      })}

      {@render button('Set Public Access', { icon: 'globe', thickness: 'solid' }, () => {
        $showAddMenu = false
        $editDialog = [null, file.file, null]
        $showEditDialog = true
      })}
    </div>
  </Overlay>
{/if}

<style lang="scss">
  div.accesses {
    flex-grow: 1;

    min-height: 0;

    padding: 8px;
    gap: 8px;

    > div.header {
      flex-direction: row;
      align-items: center;

      > h2.title {
        flex-grow: 1;

        font-weight: bolder;
        font-size: 1em;
        padding: 0 8px;
      }

      > div.actions {
        div.foreground {
          padding: 8px;
        }
      }
    }

    > div.access-list {
      flex-grow: 1;

      gap: 8px;

      min-height: 0;

      overflow: hidden auto;
    }
  }

  div.add-menu {
    background-color: var(--color-9);
    color: var(--color-1);

    div.add-menu-foreground {
      flex-grow: 1;

      padding: 8px;

      text-align: start;

      div.button {
        flex-direction: row;

        align-items: center;

        gap: 8px;
      }
    }
  }
</style>
