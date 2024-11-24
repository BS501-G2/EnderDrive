<script lang="ts">
  import { useServerContext } from '$lib/client/client'
  import AccountSettingsTab from './account-settings-tab.svelte'
  import Expando from './expando.svelte'
  import NameExpando from './name-expando.svelte'

  const server = useServerContext()
</script>

<AccountSettingsTab name="Personal Information" icon={{ icon: 'info', thickness: 'solid' }}>
  <Expando title="Name Information">
    {#await server.me() then me}
      <NameExpando {me} />
    {/await}
  </Expando>
</AccountSettingsTab>
