<script
	lang="ts"
>
	import {
		useServerContext,
		type UserResource
	} from '$lib/client/client';
	import Button from '$lib/client/ui/button.svelte';
	import Icon, {
		type IconOptions
	} from '$lib/client/ui/icon.svelte';
	import { type Snippet } from 'svelte';
	import { goto } from '$app/navigation';
	import LogoutConfirmation from './logout-confirmation.svelte';

	let {
		user,
		ondismiss,
		logoutConfirmation = $bindable()
	}: {
		user: UserResource;
		ondismiss: () => void;
		logoutConfirmation: boolean;
	} = $props();
	const {
		deauthenticate
	} =
		useServerContext();
</script>

{#snippet action(
	name: string,
	icon: IconOptions,
	onclick: () => void
)}
	{#snippet foreground(
		view: Snippet
	)}
		<div
			class="foreground"
		>
			{@render view()}
		</div>
	{/snippet}

	<Button
		onclick={() => {
			try {
				return onclick();
			} finally {
				ondismiss();
			}
		}}
		{foreground}
	>
		<Icon
			{...icon}
		/>
		<p
			class="label"
		>
			{name}
		</p>
	</Button>
{/snippet}

<div
	class="user-menu"
>
	{@render action(
		'Settings',
		{
			icon: 'gear',
			thickness:
				'solid'
		},
		() => {
			goto(
				'/app/settings'
			);
		}
	)}

	<i
		class="red"
	>
		{@render action(
			'Log out',
			{
				icon: 'right-from-bracket',
				thickness:
					'solid'
			},
			() => {
				logoutConfirmation = true;
			}
		)}
	</i>
</div>

<style
	lang="scss"
>
	div.user-menu {
		gap: 8px;

		div.foreground {
			flex-grow: 1;

			flex-direction: row;
			align-items: center;

			padding: 8px;
			gap: 8px;

			p.label {
				flex-grow: 1;

				text-align: start;
			}
		}

		i.red {
			display: contents;
			color: var(
				--color-6
			);
		}
	}
</style>
