<script
  lang="ts"
>
  import { useAppContext } from '$lib/client/contexts/app';

  const {
    title
  }: {
    title: string;
  } =
    $props();

  const {
    pushTitle
  } =
    useAppContext();

  const trimedTitle =
    $derived(
      title.trim()
    );

  $effect(
    () => {
      if (
        trimedTitle
      ) {
        return pushTitle(
          trimedTitle
        );
      }
    }
  );
</script>
