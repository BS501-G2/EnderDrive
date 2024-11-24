<script lang="ts">
  import { type Snippet } from 'svelte'
  import { useSyncContext } from '../sync'
  import Expando from './expando.svelte'
  import Button from '$lib/client/ui/button.svelte'
  import { goto } from '$app/navigation'

  const sync = useSyncContext()
</script>

{#if sync == null}
  {#if !window.location.protocol.startsWith('https')}
    <p>It seems that you are visiting this site in an insecure connection.</p>
    <p>FileSystem API is available only in secure connections (HTTPS).</p>
  {:else}
    <p>Your browser does not seem to support the newest FileSystem API</p>
    <p>This feature is only supported in latest versionso of Google Chrome and Microsoft Edge.</p>
  {/if}

  {#snippet learnMoreForeground(view: Snippet)}
    <div class="learn-more-foreground">
      {@render view()}
    </div>
  {/snippet}

  {#snippet learnMoreBackground(view: Snippet)}
    <div class="learn-more-background">
      {@render view()}
    </div>
  {/snippet}

  <Button
    foreground={learnMoreForeground}
    background={learnMoreBackground}
    onclick={() => {
      window.location.href = 'https://developer.mozilla.org/en-US/docs/Web/API/File_System_API'
    }}
  >
    Learn More
  </Button>

  <!-- <a href="https://developer.mozilla.org/en-US/docs/Web/API/File_System_API">Learn More</a> -->
{/if}

<style lang="scss">
  div.learn-more-foreground {
    padding: 8px;
  }

  div.learn-more-background {
    background-color: var(--color-1);
    color: var(--color-5);
  }

  a {
    color: inherit;
    text-decoration: none;
  }

  a:hover {
    text-decoration: underline;
  }
</style>
