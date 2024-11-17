<script
  lang="ts"
>
  import { useLandingContext } from '$lib/client/contexts/landing';
  import type { IconOptions } from '$lib/client/ui/icon.svelte';
  import {
    onMount,
    type Snippet
  } from 'svelte';

  const {
    children,
    icon,
    isSecondary = false,
    onclick
  }: {
    children: Snippet;
    icon: IconOptions;
    isSecondary?: boolean;
    onclick: () => void;
  } = $props();
  const {
    pushButton:
      pushAction
  } =
    useLandingContext();

  onMount(
    () =>
      pushAction(
        container,
        icon,
        isSecondary,
        onclick
      )
  );
</script>

{#snippet container()}
  {@render children()}
{/snippet}
