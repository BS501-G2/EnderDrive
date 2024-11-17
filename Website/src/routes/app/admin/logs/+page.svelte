<script
  lang="ts"
>
  import { useAdminContext } from '$lib/client/contexts/admin';
  import { onMount } from 'svelte';
  import Title from '../../title.svelte';

  const {
    pushTitle
  } =
    useAdminContext();

  onMount(
    () =>
      pushTitle(
        'File Logs'
      )
  );
</script>
