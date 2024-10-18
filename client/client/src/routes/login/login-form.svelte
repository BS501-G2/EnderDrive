<script lang="ts">
	import { goto } from '$app/navigation';
	import { authenticateWithPassword } from '$lib/client/client';
	import { Button, Input, ViewMode, viewMode } from '@rizzzi/svelte-commons';
	import { type Snippet } from 'svelte';
	import { writable, type Writable } from 'svelte/store';

	const { errorStore }: { errorStore: Writable<Error | null> } = $props();

	const username: Writable<string> = writable('');
	const password: Writable<string> = writable('');

	let loginButton: () => void = $state(null as never);
	let passFocus: () => void = $state(null as never);
</script>

{#snippet buttonContainer(view: Snippet)}
	<div class="button-container">{@render view()}</div>
{/snippet}

<div
	class="form"
	class:mobile={$viewMode & ViewMode.Mobile}
	class:desktop={$viewMode & ViewMode.Desktop}
>
	<div class="site-logo">
		<img src="/favicon.svg" alt="logo" />
		<h2>EnderDrive</h2>
	</div>
	<div class="fields">
		<Input
			type="text"
			icon="fa-circle-user fa-solid"
			name="Username"
			value={username}
			onSubmit={passFocus}
		/>

		<Input
			type="password"
			icon="fa-key fa-solid"
			name="Password"
			value={password}
			bind:focus={passFocus}
			onSubmit={loginButton}
		/>

		<Button
			bind:click={loginButton}
			buttonClass="primary"
			onClick={async () => {
				try {
					await authenticateWithPassword($username, $password);
					await goto('/app');
				} catch (error: any) {
					$errorStore = error;
				}
			}}
			container={buttonContainer}
		>
			Login
		</Button>
	</div>
</div>

<style lang="scss">
	div.form {
		background-color: var(--backgroundVariant);
		color: var(--onBackgroundVariant);

		min-width: 320px;
		max-width: 320px;

		padding: 16px;
		border-radius: 16px;
		gap: 8px;

		box-sizing: border-box;

		display: flex;
		flex-direction: column;

		justify-content: safe center;

		> div.fields {
			display: flex;
			flex-direction: column;
			gap: 16px;
		}

		> div.site-logo {
			display: flex;
			flex-direction: row;

			gap: 16px;
			padding: 16px;

			justify-content: safe center;
			align-items: center;

			> h2 {
				font-weight: lighter;
			}

			> img {
				width: 64px;
				height: 64px;
			}
		}
	}

	div.form.desktop {
		border-radius: 16px;
	}

	div.form.mobile {
		min-width: 0px;
		max-width: unset;
		flex-grow: 1;
		border-radius: 0px;
	}

	div.button-container {
		padding: 8px;
	}
</style>
