<script lang="ts">
	import { range } from '$lib/range';
	import { viewMode, ViewMode } from '$lib/responsive-layout.svelte';
	import { writable, type Writable } from 'svelte/store';
	import { privacyAccepted } from '../privacy-accepted';
	import Button from '$lib/widgets/button.svelte';
	import { type Snippet } from 'svelte';
	import { goto } from '$app/navigation';

	const check: Writable<boolean> = writable(false);
</script>

<div class="page-container">
	<div class="page" class:mobile={$viewMode & ViewMode.Mobile}>
		<h2>EnderDrive Privacy Policy Agreement</h2>

		<p class="content">
			{#each range(1000) as _}
				{#each range(Math.floor(Math.random() * 10)) as _}
					a
				{/each}{' '}
			{/each}
		</p>

		{#if !$privacyAccepted}
			<div class="actions" class:mobile={$viewMode & ViewMode.Mobile}>
				<div class="accept-field">
					<input
						class="checkbox"
						id="i-accept"
						type="checkbox"
						onchange={({ currentTarget }) => {
							$check = currentTarget.checked;
						}}
					/>
					<label for="i-accept">I accept EnderDrive's Privacy Policy</label>
				</div>

				{#snippet buttonContainer(view: Snippet)}
					<p class="button">{@render view()}</p>
				{/snippet}

				<Button
					onClick={async () => {
						if (!$check) {
							throw new Error('Please check the box');
						}

						$privacyAccepted = true;
						await goto('/app');
					}}
					container={buttonContainer}
				>
					Proceed
				</Button>
			</div>
		{:else}
			<p>You already have accepted this agreement.</p>
		{/if}
	</div>
</div>

<style lang="scss">
	div.page-container {
		display: flex;
		flex-direction: row;

		justify-content: safe center;

		min-width: 100dvw;
		max-width: 100dvw;
		max-height: 100dvh;
		min-height: 100dvh;

		box-sizing: border-box;

		> div.page {
			flex-grow: 1;

			display: flex;
			flex-direction: column;
			box-sizing: border-box;

			padding: 16px;
			gap: 8px;

			max-width: min(768px, 100dvw);

			> h2 {
				font-weight: lighter;

				color: var(--primary);
			}

			> p.content {
				flex-grow: 1;

				background-color: var(--backgroundVariant);
				color: var(--onBackgroundVariant);
				border: solid var(--primary) 1px;
				overflow: hidden auto;

				text-align: justify;
				text-wrap: wrap;
				word-break: break-all;

				padding: 4px 8px;
			}

			> div.actions {
				display: flex;
				flex-direction: row;

				align-items: center;

				gap: 8px;
				line-height: 1em;

				> div.accept-field {
					flex-grow: 1;

					display: flex;
					flex-direction: row;
					gap: 8px;

					> label {
						line-height: 1em;
					}
				}

				p.button {
					padding: 4px;
				}
			}

			> div.actions.mobile {
				flex-direction: column;

				align-items: unset;
			}
		}

		> div.page.mobile {
			background-color: var(--backgroundVariant);
			color: var(--onBackgroundVariant);
		}
	}
</style>
