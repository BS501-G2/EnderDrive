<script
  lang="ts"
>
  import type { Snippet } from 'svelte';

  const {
    onframe,
    children
  }: {
    onframe: (
      lastTime:
        | number
        | null,
      currentTime: number
    ) => void;
    children?: Snippet;
  } =
    $props();

  $effect(
    () => {
      let lastTime:
        | number
        | null =
        null;
      let frameRequestId:
        | number
        | null =
        null;

      function run() {
        frameRequestId =
          null;

        const currentTime =
          Date.now();

        onframe(
          lastTime,
          currentTime
        );
        lastTime =
          currentTime;

        frameRequestId =
          requestAnimationFrame(
            run
          );
      }

      frameRequestId =
        requestAnimationFrame(
          run
        );

      return () => {
        if (
          frameRequestId !=
          null
        ) {
          cancelAnimationFrame(
            frameRequestId
          );
        }
      };
    }
  );
</script>

{@render children?.()}
