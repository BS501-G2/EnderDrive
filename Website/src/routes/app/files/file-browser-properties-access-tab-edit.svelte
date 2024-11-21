<script lang="ts">
  import { FileAccessLevel, useServerContext, type UserResource } from '$lib/client/client'
  import Icon, { type IconOptions } from '$lib/client/ui/icon.svelte'
  import SelectOption from '$lib/client/ui/select-option.svelte'
  import Select from '$lib/client/ui/select.svelte'
  import Window from '$lib/client/ui/window.svelte'
  import { writable, type Writable } from 'svelte/store'

  const { user, ondismiss }: { user: UserResource; ondismiss: () => void } = $props()

  const { setFileAccess, getFileAccesses } = useServerContext()

  const fileAccessLevel: Writable<FileAccessLevel | null> = writable(null)
</script>

<Window {ondismiss} title="Set File Access for {user?.displayName ?? user?.firstName}">
  <div class="content">
    <div class="actions">
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
</Window>

<style lang="scss">
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
</style>
