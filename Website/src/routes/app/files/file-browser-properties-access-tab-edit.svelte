<script lang="ts">
  import { useClientContext } from '$lib/client/client'
  import { type FileResource, type UserResource, FileAccessLevel } from '$lib/client/resource'
  import Button from '$lib/client/ui/button.svelte'
  import Icon, { type IconOptions } from '$lib/client/ui/icon.svelte'
  import SelectOption from '$lib/client/ui/select-option.svelte'
  import Select from '$lib/client/ui/select.svelte'
  import Window from '$lib/client/ui/window.svelte'
  import { onMount, type Snippet } from 'svelte'
  import { writable, type Writable } from 'svelte/store'

  const {
    file,
    user,
    ondismiss,
    refresh,
    existingValue
  }: {
    file: FileResource
    user: UserResource | null
    refresh: () => void
    ondismiss: () => void
    existingValue?: FileAccessLevel
  } = $props()
  const { server } = useClientContext()

  const fileAccessLevel: Writable<FileAccessLevel | null> = writable(existingValue ?? null)
</script>

<Window
  {ondismiss}
  title="Set{user == null ? ' Public' : ''} File Access {user != null
    ? `for ${user.DisplayName ?? user.FirstName}`
    : null}"
>
  <div class="content">
    <div class="main">
      <div class="field">
        <div class="label">Access Level</div>
        <div class="value">
          {#snippet option(
            level: FileAccessLevel,
            name: string,
            description: string,
            icon: IconOptions
          )}
            <SelectOption label={name} value={level}>
              <div class="option">
                <div class="icon">
                  <Icon {...icon} />
                </div>
                <div class="label">
                  <p class="name">{name}</p>
                  <p class="description">{description}</p>
                </div>
              </div>
            </SelectOption>
          {/snippet}

          <Select bind:selected={$fileAccessLevel}>
            {@render option(FileAccessLevel.None, 'None', 'User has no access to this file.', {
              icon: 'circle-xmark',
              thickness: 'regular'
            })}
            {@render option(FileAccessLevel.Read, 'Read-Only', 'User can only read this file.', {
              icon: 'glasses',
              thickness: 'solid'
            })}
            {@render option(
              FileAccessLevel.ReadWrite,
              'Read & Write',
              'User can read and write this file.',
              { icon: 'pen', thickness: 'solid' }
            )}
            {@render option(
              FileAccessLevel.Manage,
              'Manage Access',
              'User can manage access to this file.',
              { icon: 'users', thickness: 'solid' }
            )}
            {@render option(FileAccessLevel.Full, 'Full Access', 'User can delete this file.', {
              icon: 'shield-halved',
              thickness: 'solid'
            })}
          </Select>
        </div>
      </div>
    </div>

    <div class="actions">
      {#snippet action(
        name: string,
        onclick: () => Promise<void>,
        primary: boolean = false,
        disabled: boolean = false
      )}
        {#snippet foreground(view: Snippet)}
          <div class="action-container" class:primary>
            {@render view()}
          </div>
        {/snippet}

        <Button {foreground} {onclick} {disabled}>
          <p>{name}</p>
        </Button>
      {/snippet}

      {@render action(
        'Save',
        async () => {
          await server.SetFileAccess({
            FileId: file.Id,
            Level: $fileAccessLevel!,
            TargetUserId: user?.Id
          })

          refresh()

          ondismiss()
        },
        true,
        $fileAccessLevel == null
      )}
      {@render action(
        'Cancel',
        async () => {
          ondismiss()
        },
        false
      )}
    </div>
  </div>
</Window>

<style lang="scss">
  @use '../../../global.scss' as *;

  div.content {
    @include force-size(min(100dvw - 64px, 480px), &);

    gap: 16px;
  }

  div.field {
    flex-direction: row;
    align-items: center;
    gap: 8px;

    > div.label {
      flex-grow: 1;
      flex-basis: 0;

      font-weight: bolder;
    }

    > div.value {
      flex-basis: 72px;
      flex-grow: 1;
    }
  }

  div.option {
    flex-direction: row;

    align-items: center;

    gap: 8px;

    > div.icon {
      align-items: center;
      justify-content: center;
    }

    > div.label {
      flex-grow: 1;

      text-align: start;

      > p.name {
        font-weight: bolder;
      }
    }
  }

  div.actions {
    flex-grow: 1;
    gap: 8px;

    flex-direction: row;
    justify-content: flex-end;

    div.action-container {
      padding: 8px;
    }

    div.action-container.primary {
      background-color: var(--color-1);
      color: var(--color-5);
    }
  }
</style>
