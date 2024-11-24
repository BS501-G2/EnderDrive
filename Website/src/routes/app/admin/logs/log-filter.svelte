<script lang="ts">
  import { writable } from 'svelte/store'
  import AdminSidePanel from '../admin-side-panel.svelte'
  import Input from '$lib/client/ui/input.svelte'
  import Button from '$lib/client/ui/button.svelte'
  import { type Snippet } from 'svelte'

  const userId = writable<string | null>(null)
</script>

{#snippet foreground(view: Snippet)}
  <div class="foreground">
    {@render view()}
  </div>
{/snippet}
<!-- <AdminSidePanel name="Filter" icon={{ icon: 'filter', thickness: 'solid' }}>
  <Button {foreground} onclick={() => {}}></Button>
</AdminSidePanel> -->

<style lang="scss">
  div.foreground {
    flex-direction: row;
    flex-grow: 1;

    padding: 8px;

    align-items: center;
  }
</style>
