<script lang="ts">
  import { useAdminContext } from '$lib/client/contexts/admin'
  import { useAppContext } from '$lib/client/contexts/app'
  import { useDashboardContext } from '$lib/client/contexts/dashboard'
  import type { IconOptions } from '$lib/client/ui/icon.svelte'
  import { onMount, type Snippet } from 'svelte'
  import AdminSidePanelDesktop from './admin-side-panel-desktop.svelte'
  import AdminSidePanelMobile from './admin-side-panel-mobile.svelte'

  const {
    name,
    icon,
    children
  }: {
    name: string
    icon: IconOptions
    children: Snippet
  } = $props()

  const { isMobile, isDesktop } = useAppContext()
</script>

{#if $isDesktop}
  <AdminSidePanelDesktop {name} {icon} {children} />
{:else if $isMobile}
  <AdminSidePanelMobile {name} {icon} {children} />
{/if}
