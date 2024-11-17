<script
	lang="ts"
>
	import { goto } from '$app/navigation';
	import { useAppContext } from '$lib/client/contexts/app';
	import NotificationHost from '../notification-host.svelte';

	const {
		pushTitle,
		isMobile
	} =
		useAppContext();

	$effect(
		() =>
			pushTitle(
				'Notifications'
			)
	);

	$effect(
		() =>
			isMobile.subscribe(
				(
					isMobile
				) => {
					if (
						!isMobile
					) {
						goto(
							'/app'
						);
					}
				}
			)
	);
</script>

<NotificationHost
/>
