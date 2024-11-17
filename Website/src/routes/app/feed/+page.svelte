<script
  lang="ts"
>
  import { useServerContext } from '$lib/client/client';
  import { useNavigationContext } from '$lib/client/contexts/navigation';
  import Separator from '$lib/client/ui/separator.svelte';
  import { loremIpsum } from 'lorem-ipsum';
  import { onMount } from 'svelte';

  const {} =
    $props();
  const {
    me
  } =
    useServerContext();
</script>

{#await me() then user}
  <div
    class="page"
  >
    <div
      class="welcome"
    >
      <h2
      >
        Welcome,
        {user!
          .firstName}!
      </h2>
    </div>

    <Separator
      horizontal
    />

    {loremIpsum(
      {
        count: 100
      }
    )}
  </div>
{/await}

<style
  lang="scss"
>
  div.page {
    margin: 64px;
    gap: 8px;

    > div.welcome {
      // font-weight: bolder;

      font-size: 2rem;
    }
  }
</style>
