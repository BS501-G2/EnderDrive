<script lang="ts">
  import { useDashboardContext } from '$lib/client/contexts/dashboard'
  import type { IconOptions } from '$lib/client/ui/icon.svelte'
  import { type Snippet } from 'svelte'
  import AppButton from '../app-button.svelte'
  import Window from '$lib/client/ui/window.svelte'
  import { writable } from 'svelte/store'

  const {
    name,
    icon,
    children
  }: {
    name: string
    icon: IconOptions
    children: Snippet
  } = $props()

  const { pushMobileAppButton } = useDashboardContext()
  const show = writable(false)
</script>

<AppButton
  label={name}
  {icon}
  onclick={() => {
    $show = true
  }}
/>

{#if $show}
  <Window
    titleIcon={icon}
    title={name}
    ondismiss={() => {
      $show = false
    }}
  >
    <div class="admin">
      {@render children()}
    </div>
  </Window>
{/if}
