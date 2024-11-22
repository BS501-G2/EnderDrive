<script lang="ts">
  import { persisted } from 'svelte-persisted-store'
  import AccountSettingsTab from './account-settings-tab.svelte'
  import { getFileSyncStorage } from './account-settings'

  getFileSyncStorage()
</script>

<AccountSettingsTab
  name="Offline Availability"
  icon={{ icon: 'wifi', thickness: 'solid' }}
></AccountSettingsTab>
