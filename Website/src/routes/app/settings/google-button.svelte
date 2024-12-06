<script lang="ts">
  import { useClientContext } from '$lib/client/client'
  import Button from '$lib/client/ui/button.svelte'
  import { create } from '$lib/client/utils'
  import { onMount } from 'svelte'

  const { server } = useClientContext()
  const { refresh }: { refresh: () => void } = $props()

  let button: HTMLDivElement = $state(null as never)
  let token: string | null = $state(null as never)
  let ex: HTMLButtonElement = $state(null as never)

  onMount(() => {
    create(button, async (newToken) => {
      token = newToken
      ex.click()
    })
  })
</script>

<div bind:this={button}></div>

<div hidden>
  <Button
    bind:buttonElement={ex}
    onclick={async () => {
      await server.SetGoogleAuthentication({
        Token: token!
      })
      refresh()
    }}
  >
    ...
  </Button>
</div>
