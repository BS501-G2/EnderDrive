<script
	lang="ts"
>
	import { useAppContext } from '$lib/client/contexts/app';
	import Favicon from '$lib/client/ui/favicon.svelte';

	const {
		isDesktop,
		isMobile
	} =
		useAppContext();
</script>

<div
	class="site-logo"
>
	<Favicon
		size={32}
	/>
</div>

<style
	lang="scss"
>
	div.site-logo {
		flex-direction: row;
		align-items: center;

		> p {
			font-size: 1.5rem;
			// font-weight: lighter;
		}
	}
</style>
