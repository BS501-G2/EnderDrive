<script lang="ts">
  import type { FileProperties } from '$lib/client/contexts/file-browser'
  import { writable, type Writable } from 'svelte/store'
  import FileBrowserPropertiesTab from './file-browser-properties-tab.svelte'
  import {
    FileAccessLevel,
    FileAccessTargetEntityType,
    useServerContext,
    type FileAccessResource,
    type UserResource
  } from '$lib/client/client'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import Button from '$lib/client/ui/button.svelte'
  import { type Snippet } from 'svelte'
  import Icon from '$lib/client/ui/icon.svelte'
  import FileBrowserPropertiesAccessTabAdd from './file-browser-properties-access-tab-add.svelte'
  import FileBrowserPropertiesAccessTabEdit from './file-browser-properties-access-tab-edit.svelte'

  const { file }: { file: FileProperties } = $props()
  const { getFileAccesses, getUser } = useServerContext()

  interface FileAccessEntry {
    access: FileAccessResource
    user: UserResource | null
  }

  async function load(): Promise<FileAccessEntry[]> {
    const fileAccesses = await getFileAccesses(
      void 0,
      file.file.id,
      void 0,
      FileAccessLevel.Read,
      void 0
    )

    return await Promise.all(
      fileAccesses.map(async (access): Promise<FileAccessEntry> => {
        const user =
          access.targetEntity?.entityType === FileAccessTargetEntityType.User
            ? await getUser(access.targetEntity.entityId)
            : null

        return {
          access,
          user
        }
      })
    )
  }

  const promise: Writable<Promise<FileAccessEntry[]>> = writable(load())
  const addDialog = writable<boolean>(false)
  const editDialog = writable<[user: UserResource] | null>(null)
</script>

{#if $addDialog}
  <FileBrowserPropertiesAccessTabAdd
    ondismiss={() => {
      $addDialog = false
    }}
    onresult={(user) => {
      $editDialog = [user]
      $addDialog = false
    }}
  />
{/if}

{#if $editDialog != null}
  {@const [user] = $editDialog ?? []}
  {#if user != null}
    <FileBrowserPropertiesAccessTabEdit {user} ondismiss={() => ($editDialog = null)} />
  {/if}
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
            $addDialog = true
          }}
        >
          <Icon icon="add" thickness="solid" />
        </Button>
      </div>
    </div>
    <div class="access-list">
      {#await $promise}
        <LoadingSpinner size="3em" />
      {:then accesses}
        {#each accesses as access}
          <p>{JSON.stringify(access)}</p>
        {/each}
      {/await}
    </div>
  </div>
</FileBrowserPropertiesTab>

<style lang="scss">
  div.accesses {
    flex-grow: 1;

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

      overflow: hidden auto;
    }
  }
</style>
