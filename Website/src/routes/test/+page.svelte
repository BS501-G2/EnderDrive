<script lang="ts">
	import { useClientContext } from '$lib/client/client';
	import Button from '$lib/client/ui/button.svelte';
	import { onMount, type Snippet } from 'svelte';

	const {
		functions: { getSetupRequirements, createAdmin }
	} = useClientContext();

	let actions: { id: number; name: string; onClick: () => Promise<any> }[] = $state([]);

	let output: any = $state(null);

	const pushAction = (name: string, onClick: () => Promise<any>) => {
		const id = Math.random();

		actions = [
			...actions,
			{
				id,
				name,
				onClick: async () => {
					try {
						const result = await onClick();
						
						return (output = typeof result === 'string' ? result : JSON.stringify(result));
					} catch (error: any) {
						output = `${error.message}\n${error.stack}`;

						throw error;
					}
				}
			}
		];

		return () => {
			actions = actions.filter((action) => action.id !== id);
		};
	};

	const credentials = {
		Username: 'testuser',
		Password: 'testuser123;',
		ConfirmPassword: 'testuser123;'
	};
	onMount(() => pushAction('Get Setup Requirements', () => getSetupRequirements({})));
	onMount(() =>
		pushAction('Create Administrator Account', async () => {
			await createAdmin({
				...credentials,
				FirstName: 'Test',
				MiddleName: null,
				DisplayName: null,
				LastName: 'User'
			});
		})
	);
</script>

{#snippet background(view: Snippet)}
	<div class="button">
		{@render view()}
	</div>
{/snippet}

{#snippet foreground(view: Snippet, error: boolean)}
	<p class="button">
		{@render view()}
	</p>
{/snippet}

<div class="card">
	<div class="header">
		<h2>Actions</h2>
	</div>
	<div class="separator"></div>
	<div class="body actions">
		{#each actions as { id, name, onClick } (id)}
			<Button {background} onclick={onClick} {foreground}>{name}</Button>
		{/each}
	</div>
</div>

<div class="card">
	<div class="header">
		<h2>Output</h2>
	</div>
	<div class="separator"></div>
	<div class="body">
		<pre>{output}</pre>
	</div>
</div>

<style lang="scss">
	@use '../global.scss' as *;

	div.button {
		background-color: var(--color-5);
	}

	p.button {
		padding: 8px;
	}

	div.card {
		padding: 16px;
		margin: 16px;
		gap: 16px;
		border-radius: 16px;

		background-color: var(--color-9);

		> div.separator {
			background-color: var(--color-1);

			@include force-size(&, 1px);
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
