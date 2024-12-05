<script lang="ts">
  import {
    useDashboardContext,
    type BackgroundTask,
    type BackgroundTaskState
  } from '$lib/client/contexts/dashboard'
  import Button from '$lib/client/ui/button.svelte'
  import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte'
  import { onMount, type Snippet } from 'svelte'
  import Overlay from '../overlay.svelte'
  import { fly } from 'svelte/transition'
  import Separator from '$lib/client/ui/separator.svelte'
  import { derived, get, writable, type Readable, type Writable } from 'svelte/store'
  import Icon from '$lib/client/ui/icon.svelte'
  import ProgressHostEntry from './progress-host-entry.svelte'

  const {
    tasks
  }: {
    tasks: Readable<BackgroundTask[]>
  } = $props()
  const { pushDesktopSide } = useDashboardContext()

  $effect(() => pushDesktopSide(desktop))

  interface FlattenedBackgroundTask {
    id: number

    task: BackgroundTask
    state: BackgroundTaskState
  }

  const showOverlay = writable<boolean>(false)
  const flattenedTasks: Writable<FlattenedBackgroundTask[]> = writable([])

  const activeTasks: Readable<FlattenedBackgroundTask[]> = derived(
    flattenedTasks,
    (flattenedArray) =>
      flattenedArray.filter((flattenedEntry) => flattenedEntry.state.status[0] === 'pending')
  )

  {
    const onDestroy: (() => void)[] = []
    function update() {
      for (const destroy of onDestroy) {
        destroy()
      }

      onDestroy.splice(0)

      const flattenedArray: FlattenedBackgroundTask[] = []

      for (const task of get(tasks)) {
        const flattenedEntry: FlattenedBackgroundTask = {
          id: task.id,
          task,
          state: get(task.state)
        }

        flattenedArray.push(flattenedEntry)

        onDestroy.push(
          task.state.subscribe((state) => {
            flattenedEntry.state = state

            flattenedTasks.set(flattenedArray)
          })
        )
      }

      flattenedTasks.set(flattenedArray)
    }

    $effect(() => tasks.subscribe(() => update()))
  }
</script>

{#snippet desktop()}
  <div class="progress">
    {#snippet foreground(view: Snippet)}
      <div class="foreground">
        {@render view()}
      </div>
    {/snippet}

    {#if $flattenedTasks.length > 0}
      <Button
        onclick={() => {
          $showOverlay = true
        }}
        {foreground}
      >
        {#if $activeTasks.length > 0}
          <LoadingSpinner size="2rem" />
        {:else}
          <Icon icon="circle-check" size="2rem" />
        {/if}
      </Button>
    {/if}
  </div>
{/snippet}

{#if $showOverlay}
  <Overlay
    ondismiss={() => {
      $showOverlay = false
    }}
    x={0}
    y={0}
    notransition
  >
    {#snippet children(windowButtons: Snippet)}
      <div
        class="overlay"
        transition:fly|global={{
          x: -360
        }}
      >
        <div class="header">
          <h2>Operations</h2>

          {@render windowButtons()}
        </div>

        <Separator horizontal />

        <div class="main">
          {#each $flattenedTasks as { id, task, state }, index (id)}
            {#if index === 0}
              <Separator horizontal />
            {/if}

            <ProgressHostEntry {task} />
          {/each}
        </div>
      </div>
    {/snippet}
  </Overlay>
{/if}

<style lang="scss">
  @use '../../global.scss' as *;

  div.progress {
    flex-direction: row;

    justify-content: safe center;
    padding: 16px 0;

    div.foreground {
      padding: 8px;

      color: var(--color-1);
    }
  }

  div.overlay {
    background-color: var(--color-9);
    color: var(--color-1);

    @include force-size(min(calc(100dvw - 64px), 360px), 100dvh);

    > div.header {
      flex-direction: row;
      align-items: center;

      > h2 {
        margin: 8px;
        font-weight: bolder;
        font-size: 1.5em;
        flex-grow: 1;
      }
    }

    > div.main {
      gap: 8px;

      overflow: hidden auto;
    }
  }
</style>
