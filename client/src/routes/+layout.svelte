<script lang="ts">
  import '@fortawesome/fontawesome-free/css/all.min.css';
  import Locale, { LocaleKey } from '$lib/locale.svelte';
  import {
    ColorKey,
    ResetCSS,
    Title,
    titleString,
    ColorScheme,
    getColorHex,
    currentColorScheme,
    ResponsiveLayout
  } from '@rizzzi/svelte-commons';
  import type { Snippet } from 'svelte';

  const { children }: { children: Snippet } = $props();
</script>

<svelte:head>
  <meta name="theme-color" content={getColorHex(ColorKey.PrimaryContainer, $currentColorScheme)} />
  <title>{$titleString}</title>
</svelte:head>

<ResponsiveLayout />
<Locale string={[[LocaleKey.AppName]]}>
  {#snippet children([appName]: string[])}
    <Title title={appName} />
  {/snippet}
</Locale>
<ResetCSS />
<ColorScheme colorScheme={$currentColorScheme} />

{@render children()}

<style lang="scss">
  :root {
    font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;

    background-color: var(--background);

    color: var(--onBackground);
    user-drag: none;
    -webkit-user-drag: none;
    user-select: none;
    -moz-user-select: none;
    -webkit-user-select: none;
    -ms-user-select: none;

    min-width: 320px;
  }

  :global(body) {
    -webkit-app-region: drag;

    margin: unset;
    min-height: 100dvh;
    background-color: var(--background);
  }
</style>
