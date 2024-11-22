<script lang="ts">
  import AccountSettingsTab from './account-settings-tab.svelte'
  import Expando from './expando.svelte'
  import GoogleAuthenticationExpando from './google-authentication-expando.svelte'
  import UpdatePasswordExpando from './update-password-expando.svelte'
  import UpdateUsernameExpando from './update-username-expando.svelte'
</script>

<AccountSettingsTab name="Login Information" icon={{ icon: 'key', thickness: 'solid' }}>
  <Expando title="Username">
    <UpdateUsernameExpando />
  </Expando>
  <Expando title="Password">
    <UpdatePasswordExpando />
  </Expando>
  <Expando title="Google Authentication">
    <GoogleAuthenticationExpando />
  </Expando>
</AccountSettingsTab>
