<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { useAppContext } from '$lib/client/contexts/app';
	import { useLandingContext, type LoginContext } from '$lib/client/contexts/landing';
	import { delay } from '$lib/client/promise-source';
	import Button from '$lib/client/ui/button.svelte';
	import Dialog from '$lib/client/ui/dialog.svelte';
	import Favicon from '$lib/client/ui/favicon.svelte';
	import Icon, { type IconOptions } from '$lib/client/ui/icon.svelte';
	import Input from '$lib/client/ui/input.svelte';
	import Overlay from '$lib/client/ui/overlay.svelte';
	import { onMount, type Snippet } from 'svelte';
	import { derived, writable, type Writable } from 'svelte/store';

	const { isDesktop, isMobile, isLimitedDesktop } = useAppContext();

	const {
		login,
		windowButtons: overlayButtons
	}: { login: Writable<LoginContext>; windowButtons: Snippet } = $props();
	const { username, password } = $login;

	interface AlternativeAction {
		id: number;
		name: string;
		icon: IconOptions;
		onclick: () => Promise<void>;
	}

	const actions: Writable<AlternativeAction[]> = writable([]);
	function pushAction(name: string, icon: IconOptions, onclick: () => Promise<void>): () => void {
		const id = Math.random();

		actions.update((actions) => [...actions, { id, name, icon, onclick }]);

		return () => actions.update((actions) => actions.filter((action) => action.id !== id));
	}

	const redirectPath = derived(page, (page) => page.url.searchParams.get('return') ?? null);

	async function redirect() {
		if ($redirectPath != null) {
			await goto($redirectPath);
		}
	}

	let button: () => void = $state(() => {});

	onMount(() => pushAction('Google', { brand: true, icon: 'google' }, async () => {}));
	onMount(() => pushAction('Reset Password', { thickness: 'solid', icon: 'key' }, async () => {}));
</script>

<div
	class="login"
	class:desktop={$isDesktop}
	class:limited={$isLimitedDesktop}
	class:mobile={$isMobile}
>
	{#if !$isMobile}
		<div class="bullettin"></div>
	{/if}

	<div class="side" class:mobile={$isMobile} class:desktop={$isDesktop}>
		<div class="overlay-buttons">
			{@render overlayButtons()}
		</div>
		<div class="form" class:mobile={$isMobile} class:desktop={$isDesktop}>
			<div class="logo">
				<Favicon size={64} />

				<p>EnderDrive</p>
			</div>

			<div class="field">
				<Input
					icon={{ icon: 'user', thickness: 'solid' }}
					id="username"
					type="text"
					name="Username"
					bind:value={$username}
					onSubmit={() => button?.()}
				/>

				<Input
					icon={{ icon: 'key', thickness: 'solid' }}
					id="password"
					type="text"
					name="Password"
					bind:value={$password}
					onSubmit={() => button?.()}
				/>

				{#snippet container(view: Snippet)}
					<div class="submit">
						{@render view()}
					</div>
				{/snippet}

				<Button
					{container}
					bind:click={button}
					onclick={async () => {
						await delay(1000);
					}}
				>
					<p>Login</p>
				</Button>
			</div>

			<div class="choices">
				<div class="or">
					<div class="line"></div>
					<p>or</p>
					<div class="line"></div>
				</div>

				<div class="actions">
					{#snippet container(view: Snippet)}
						<div class="action-container">
							{@render view()}
						</div>
					{/snippet}

					{#each $actions as { id, name, icon, onclick } (id)}
						<Button {container} {onclick}>
							<div class="action">
								<Icon {...icon} />
								<p>{name}</p>
							</div>
						</Button>
					{/each}
				</div>
			</div>
		</div>
	</div>
</div>

<style lang="scss">
	@use '../global.scss' as *;

	div.login {
		flex-direction: row;
		background-color: var(--color-5);
		box-shadow: 2px 2px 8px var(--color-10);
		border-radius: 8px;

		overflow: hidden;

		@include force-size(min(75dvw, 1280px), min(75dvh, 720px));

		div.bullettin {
			flex-grow: 1;
			flex-shrink: 0;
		}

		> div.side {
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

				> div.field {
					gap: 16px;

					justify-content: safe center;

					div.submit {
						align-items: center;
						justify-content: center;

						background-color: var(--color-1);
						color: var(--color-5);
						flex-grow: 1;

						min-height: 32px;
					}
				}

				> div.choices {
					gap: 16px;

					> div.or {
						flex-direction: row;
						align-items: center;

						gap: 8px;

						div.line {
							flex-grow: 1;

							@include force-size(&, 1px);

							background-color: var(--color-1);
						}
					}

					> div.actions {
						display: grid;
						gap: 8px;

						grid-template-columns: repeat(2, calc(50% - 4px));

						div.action-container {
							background-color: transparent;
							color: var(--color-1);
							border: solid 1px var(--color-1);
							border-radius: 8px;

							padding: 8px;
							flex-grow: 1;
						}

						div.action {
							flex-grow: 1;
							flex-direction: row;
							align-items: center;

							gap: 8px;

							> p {
								flex-grow: 1;
							}
						}
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

	div.login.mobile,
	div.login.limited {
		@include force-size(100dvw, 100dvh);
		border-radius: 0;
	}
</style>
