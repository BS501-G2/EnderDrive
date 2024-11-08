<script lang="ts">
	import { useAppContext } from '$lib/client/contexts/app';
	import { type LoginContext } from '$lib/client/contexts/landing';
	import Favicon from '$lib/client/ui/favicon.svelte';
	import { type Snippet } from 'svelte';
	import { type Writable } from 'svelte/store';
	import LoginForm from './login-form.svelte';

	const { isDesktop, isMobile } = useAppContext();

	const {
		login,
		windowButtons: overlayButtons
	}: { login: Writable<LoginContext>; windowButtons: Snippet } = $props();
	const { username, password } = $login;
</script>

<div class="login" class:desktop={$isDesktop} class:mobile={$isMobile}>
	<div class="side" class:mobile={$isMobile} class:desktop={$isDesktop}>
		<div class="overlay-buttons">
			{@render overlayButtons()}
		</div>
		<div class="form" class:mobile={$isMobile} class:desktop={$isDesktop}>
			<div class="logo">
				<Favicon size={64} />

				<p>EnderDrive</p>
			</div>

			<LoginForm {username} {password} />
		</div>
	</div>
</div>

<style lang="scss">
	@use '../../global.scss' as *;

	div.login {
		flex-direction: row;
		background-color: var(--color-5);
		box-shadow: 2px 2px 8px var(--color-10);

		overflow: hidden;

		> div.side {
			flex-grow: 1;

			background-color: var(--color-9);

			> div.overlay-buttons {
				flex-direction: row;

				justify-content: flex-end;
			}

			> div.form {
				flex-grow: 1;

				justify-content: safe center;

				gap: 16px;
				padding: 16px;

				> div.logo {
					flex-direction: row;
					align-items: center;
					justify-content: center;

					gap: 8px;

					> p {
						font-size: 2rem;
						font-weight: lighter;
					}
				}
			}

			> div.form.mobile {
				flex-grow: 1;
			}
		}

		> div.side.desktop {
			@include force-size(320px, &);
		}
	}
</style>
