<script lang="ts">
  import { persisted } from 'svelte-persisted-store'
  import AccountSettingsTab from './account-settings-tab.svelte'
  import SyncExpando from './sync-expando.svelte'
  import Expando from './expando.svelte'
  import { useSyncContext } from '../sync'

  const sync = useSyncContext()
</script>

<AccountSettingsTab name="EnderDrive Sync" icon={{ icon: 'rotate', thickness: 'solid' }}>
  <Expando title="Synced Folders{sync == null ? ' (Unavailable)' : null}">
    <SyncExpando />
  </Expando>
</AccountSettingsTab>
