<script
	lang="ts"
>
	import { goto } from '$app/navigation';
	import {
		useClientContext,
		useServerContext
	} from '$lib/client/client';
	import Button from '$lib/client/ui/button.svelte';
	import RequireClient from '$lib/client/ui/require-client.svelte';
	import {
		onMount,
		type Snippet
	} from 'svelte';

	let actions: {
		id: number;
		name: string;
		onClick: () => Promise<any>;
	}[] =
		$state(
			[]
		);

	let output: any =
		$state(
			null
		);

	const pushAction =
		(
			name: string,
			onClick: () => Promise<any>
		) => {
			const id =
				Math.random();

			actions =
				[
					...actions,
					{
						id,
						name,
						onClick:
							async () => {
								try {
									const result =
										await onClick();

									return (output =
										typeof result ===
										'string'
											? result
											: JSON.stringify(
													result
												));
								} catch (error: any) {
									output = `${error.stack}`;

									throw error;
								}
							}
					}
				];

			return () => {
				actions =
					actions.filter(
						(
							action
						) =>
							action.id !==
							id
					);
			};
		};

	const adminUsername =
		'testuser';
	const adminPassword =
		'TestUser123;';

	const {
		getSetupRequirements,
		createAdmin,
		resolveUsername,
		authenticatePassword
	} =
		useServerContext();

	onMount(
		() =>
			pushAction(
				'Get Setup Requirements',
				() =>
					getSetupRequirements()
			)
	);

	onMount(
		() =>
			pushAction(
				'Create Administrator Account',
				async () => {
					await createAdmin(
						adminUsername,
						adminPassword,
						adminPassword,
						'Test',
						null,
						'User',
						null
					);
				}
			)
	);

	onMount(
		() =>
			pushAction(
				'Resolve Username',
				async () =>
					resolveUsername(
						adminUsername
					)
			)
	);

	onMount(
		() =>
			pushAction(
				'Authenticate Password',
				async () => {
					const userId =
						await resolveUsername(
							adminUsername
						);

					if (
						userId ==
						null
					) {
						throw new Error(
							'User not found.'
						);
					}

					await authenticatePassword(
						userId,
						adminPassword
					);
				}
			)
	);

	onMount(
		() =>
			pushAction(
				'Go To Landing',
				() =>
					goto(
						'/landing'
					)
			)
	);

	onMount(
		() =>
			pushAction(
				'Go To App',
				() =>
					goto(
						'/app/files'
					)
			)
	);
</script>

{#snippet background(
	view: Snippet
)}
	<div
		class="button"
	>
		{@render view()}
	</div>
{/snippet}

{#snippet foreground(
	view: Snippet
)}
	<p
		class="button"
	>
		{@render view()}
	</p>
{/snippet}

<RequireClient
	nosetup
>
	<div
		class="card"
	>
		<div
			class="header"
		>
			<h2
			>
				Actions
			</h2>
		</div>
		<div
			class="separator"
		></div>
		<div
			class="body actions"
		>
			{#each actions as { id, name, onClick } (id)}
				<Button
					{background}
					onclick={onClick}
					{foreground}
					>{name}</Button
				>
			{/each}
		</div>
	</div>

	<div
		class="card"
	>
		<div
			class="header"
		>
			<h2
			>
				Output
			</h2>
		</div>
		<div
			class="separator"
		></div>
		<div
			class="body"
		>
			<pre>{output}</pre>
		</div>
	</div>
</RequireClient>

<style
	lang="scss"
>
	@use '../../global.scss'
		as *;

	div.button {
		background-color: var(
			--color-5
		);
	}

	p.button {
		padding: 8px;
	}

	div.card {
		padding: 16px;
		margin: 16px;
		gap: 16px;

		> div.separator {
			background-color: var(
				--color-1
			);

			@include force-size(
				&,
				1px
			);
		}

		> div.body.actions {
			flex-direction: row;
			flex-wrap: wrap;

			gap: 8px;
		}
	}

	pre {
		font: revert;
	}
</style>
