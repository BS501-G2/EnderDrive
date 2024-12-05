<script lang="ts">
  import type { BackgroundTask, BackgroundTaskState } from '$lib/client/contexts/dashboard'
  import Button from '$lib/client/ui/button.svelte'
  import Icon, { type IconOptions } from '$lib/client/ui/icon.svelte'
  import { onMount, type Snippet } from 'svelte'
  import { tweened } from 'svelte/motion'
  import { derived, writable } from 'svelte/store'

  const {
    task
  }: {
    task: BackgroundTask
  } = $props()
  const { state: taskState } = task

  const entryElement = writable<HTMLDivElement | null>(null)
  const hover = writable<boolean>(false)

  $effect(() => {
    if (!$entryElement) {
      return
    }

    const onmouseover = () => {
      $hover = true
    }

    const onmouseleave = () => {
      $hover = false
    }

    $entryElement.addEventListener('mouseover', onmouseover)
    $entryElement.addEventListener('mouseleave', onmouseleave)

    return () => {
      if (!$entryElement) {
        return
      }

      $entryElement.removeEventListener('mouseover', onmouseover)
      $entryElement.removeEventListener('mouseleave', onmouseleave)
    }
  })

  const tweenedProgress = tweened(0, {
    duration: 250
  })

  onMount(() =>
    taskState.subscribe((taskState) =>
      tweenedProgress.set(
        taskState.progress != null ? taskState.progress[0] / taskState.progress[1] : 0
      )
    )
  )
</script>

<div
  bind:this={$entryElement}
  class="progress-entry"
  class:hover={$hover}
  class:failed={$taskState.status[0] === 'error'}
>
  <div class="top">
    <h2>{$taskState.title}</h2>

    {#if !$hover}
      <p>{$taskState.status[0] === 'pending' ? $taskState.message : 'Task Completed'}</p>
    {:else}
      {#snippet action(name: string, icon: IconOptions, onclick: () => void)}
        {#snippet foreground(view: Snippet)}
          <div class="action-foreground">
            {@render view()}
          </div>
        {/snippet}

        <Button {onclick} {foreground} hint={name}>
          <Icon {...icon} />
        </Button>
      {/snippet}

      <div class="actions">
        {#if $taskState.status[0] !== 'pending'}
          {@render action('Dismiss', { icon: 'xmark', thickness: 'solid' }, () => task.clear())}
        {/if}
      </div>
    {/if}
  </div>

  {#if $taskState.status[0] === 'pending'}
    <div class="middle">
      <progress value={$tweenedProgress}></progress>
    </div>
  {/if}

  {#if $taskState.status[0] === 'pending'}

  {#if $taskState.footerLeft != null || $taskState.footerRight != null}
    <div class="bottom">
      <p class="bottom-text left">{$taskState.footerLeft}</p>
      <p class="bottom-text right">{$taskState.footerRight}</p>
    </div>
  {/if}
  {:else if $taskState.status[0] === 'error'}
    <div class="bottom">
      <p class="bottom-text">{$taskState.status[1].message}</p>
    </div>
  {/if}
</div>

<style lang="scss">
  @use '../../global.scss' as *;

  div.progress-entry {
    padding: 8px;

    > div.top {
      flex-direction: row;

      > h2 {
        flex-grow: 1;

        font-weight: bolder;
        font-size: 1em;
      }

      > p {
        text-align: end;

        flex-grow: 1;

        font-style: italic;
      }

      > div.actions {
        flex-direction: row;

        align-items: center;

        div.action-foreground {
          padding: 2px;
        }
      }
    }

    > div.middle {
      > progress {
        width: 100%;
      }
    }

    > div.bottom {
      flex-direction: row;

      > p.bottom-text {
        flex-grow: 1;

        font-style: italic;

      }

      > p.bottom-text.left {
        text-align: start;

        text-overflow: ellipsis;
        overflow: hidden;
        white-space: nowrap;
      }

      > p.bottom-text.right {
        text-align: end;

        overflow: hidden;
        white-space: nowrap;
      }
    }
  }

  div.progress-entry.hover {
    background-color: var(--color-5);
  }

  div.progress-entry.failed {
    color: var(--color-6);
  }
</style>
