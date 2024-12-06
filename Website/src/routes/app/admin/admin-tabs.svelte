<script lang="ts">
  import Title from '../title.svelte'
  import AdminTab from './admin-tab.svelte'
</script>

<AdminTab
  name="News"
  callback={({ url }) => {
    if (url.pathname === '/app/admin/news') {
      return {
        path: '/app/admin/news',
        selected: true,
        icon: {
          icon: 'newspaper',
          thickness: 'solid'
        }
      }
    } else {
      return {
        path: '/app/admin/news',
        selected: false,
        icon: {
          icon: 'newspaper',
          thickness: 'regular'
        }
      }
    }
  }}
/>

<AdminTab
  name="Teachers"
  callback={({ url }) => {
    if (url.pathname === '/app/admin/teachers') {
      return {
        path: '/app/admin/teachers',
        selected: true,
        icon: {
          icon: 'user',
          thickness: 'solid'
        }
      }
    } else {
      return {
        path: '/app/admin/teachers',
        selected: false,
        icon: {
          icon: 'user',
          thickness: 'regular'
        }
      }
    }
  }}
/>

<AdminTab
  name="Logs"
  callback={({ url }) => {
    if (url.pathname === '/app/admin/logs') {
      return {
        path: '/app/admin/logs',
        selected: true,
        icon: {
          icon: 'scroll',
          thickness: 'solid'
        }
      }
    } else {
      return {
        path: '/app/admin/logs',
        selected: false,
        icon: {
          icon: 'scroll',
          thickness: 'solid'
        }
      }
    }
  }}
/>
<!--
<AdminTab
  name="Password Reset Requests"
  callback={({ url }) => {
    const isPath = url.pathname === '/app/admin/reset'

    return {
      path: '/app/admin/reset',
      selected: isPath,
      icon: {
        icon: 'key',
        thickness: 'solid'
      }
    }
  }}
/> -->
