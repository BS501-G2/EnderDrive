<script
  lang="ts"
>
  import Title from '../title.svelte';
  import AdminTab from './admin-tab.svelte';
</script>

<AdminTab
  name="News"
  callback={({
    url
  }) => {
    if (
      url.pathname ===
      '/app/admin/news'
    ) {
      return {
        path: '/app/admin/news',
        selected: true,
        icon: {
          icon: 'newspaper',
          thickness:
            'solid'
        }
      };
    } else {
      return {
        path: '/app/admin/news',
        selected: false,
        icon: {
          icon: 'newspaper',
          thickness:
            'regular'
        }
      };
    }
  }}
/>

<AdminTab
  name="Users"
  callback={({
    url
  }) => {
    if (
      url.pathname ===
      '/app/admin/users'
    ) {
      return {
        path: '/app/admin/users',
        selected: true,
        icon: {
          icon: 'user',
          thickness:
            'solid'
        }
      };
    } else {
      return {
        path: '/app/admin/users',
        selected: false,
        icon: {
          icon: 'user',
          thickness:
            'regular'
        }
      };
    }
  }}
/>

<AdminTab
  name="Logs"
  callback={({
    url
  }) => {
    if (
      url.pathname ===
      '/app/admin/logs'
    ) {
      return {
        path: '/app/admin/logs',
        selected: true,
        icon: {
          icon: 'scroll',
          thickness:
            'solid'
        }
      };
    } else {
      return {
        path: '/app/admin/logs',
        selected: false,
        icon: {
          icon: 'scroll',
          thickness:
            'solid'
        }
      };
    }
  }}
/>
