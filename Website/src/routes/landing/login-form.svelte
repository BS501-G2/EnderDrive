<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import Button from '$lib/client/ui/button.svelte';
	import Icon, { type IconOptions } from '$lib/client/ui/icon.svelte';
	import Input from '$lib/client/ui/input.svelte';
	import RequireClient from '$lib/client/ui/require-client.svelte';
	import { onMount, type Snippet } from 'svelte';
	import { writable, derived, type Writable } from 'svelte/store';

	const { username, password }: { username: Writable<string>; password: Writable<string> } =
		$props();

	let clickButton: () => void = $state(() => {});

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

	onMount(() => pushAction('Google', { brand: true, icon: 'google' }, async () => {}));
	onMount(() => pushAction('Reset Password', { thickness: 'solid', icon: 'key' }, async () => {}));

	async function onclick() {
		await new Promise<void>((resolve) => setTimeout(resolve, 1000));
	}
</script>

<RequireClient>
	<div class="field">
		<Input
			icon={{ icon: 'user', thickness: 'solid' }}
			id="username"
			type="text"
			name="Username"
			bind:value={$username}
			onSubmit={clickButton}
		/>

		<Input
			icon={{ icon: 'key', thickness: 'solid' }}
			id="password"
			type="text"
			name="Password"
			bind:value={$password}
			onSubmit={clickButton}
		/>

		{#snippet container(view: Snippet)}
			<div class="submit">
				{@render view()}
			</div>
		{/snippet}

		<Button background={container} bind:click={clickButton} {onclick}>
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
				<Button background={container} {onclick}>
					<div class="action">
						<Icon {...icon} />
						<p>{name}</p>
					</div>
				</Button>
			{/each}
		</div>
	</div>
</RequireClient>

<style lang="scss">
	@use '../global.scss' as *;

	div.field {
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

	div.choices {
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
</style>
