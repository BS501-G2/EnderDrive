<script lang="ts">
	import { getConnection } from '$lib/client/client';
	import Icon from '$lib/ui/icon.svelte';
	import { Button, Dialog, Input } from '@rizzzi/svelte-commons';
	import type { Readable } from 'svelte/motion';
	import { derived, writable, type Writable } from 'svelte/store';

	const { dismiss, refresh }: { dismiss: () => void; refresh: () => void } = $props();
	const {
		serverFunctions: { createNews }
	} = getConnection();

	let fileSelector: HTMLInputElement = $state(null as never);

	const title: Writable<string> = writable('');
	const image: Writable<Blob | null> = writable(null);
	const imageUrl: Readable<string | null> = derived(image, (image) =>
		image != null ? URL.createObjectURL(image) : null
	);
</script>

{#snippet head()}
	<div class="head">
		<b class="title">Create New</b>

		<Button
			onClick={async () => {
				if ($image == null) {
					throw new Error('Banner is required');
				}

				const newsBanner = new Uint8Array(await $image.arrayBuffer());
				const newsTitle = $title;
				dismiss();

				await createNews({ title: newsTitle, banner: newsBanner });
				refresh();
			}}
		>
			<div class="create-button">
				<Icon icon="plus" />
				<p>Create New</p>
			</div>
		</Button>
	</div>
{/snippet}

<input
	class="file"
	bind:this={fileSelector}
	type="file"
	onchange={({ currentTarget }) => {
		$image = currentTarget.files!.item(0);
	}}
/>

{#snippet body()}
	<div class="body">
		<div class="field">
			<div class="title-field">
				<Input type="text" name="Title" value={title} />
			</div>

			<Button onClick={() => fileSelector!.click()}>
				<div class="add-button">
					<Icon icon="plus" />

					{#if $imageUrl != null}
						<p>Replace Banner</p>
					{:else}
						<p>Set Banner</p>
					{/if}
				</div>
			</Button>
		</div>

		{#if $imageUrl != null}
			<img src={$imageUrl} alt="Banner" />
		{/if}
	</div>
{/snippet}

<Dialog onDismiss={dismiss} {head} {body} />

<style lang="scss">
	input.file {
		display: none;
	}

	div.head {
		display: flex;
		flex-direction: row;

		align-items: center;

		> b.title {
			flex-grow: 1;
		}

		div.create-button {
			display: flex;
			flex-direction: row;
			align-items: center;
			line-height: 1em;

			gap: 4px;

			padding: 4px;
		}
	}

	div.body {
		display: flex;
		flex-direction: column;

		min-width: min(512px, 50dvw);
		max-width: min(512px, 50dvw);

		> div.field {
			display: flex;
			flex-direction: row;

			min-width: 0px;

			align-items: center;
			line-height: 1em;

			gap: 8px;

			> div.title-field {
				flex-grow: 1;

				min-width: 0px;

				display: flex;
				flex-direction: column;
			}

			div.add-button {
				display: flex;
				flex-direction: row;

				align-items: center;
				line-height: 1em;

				gap: 4px;
				padding: 4px;
			}
		}

		> img {
			min-width: 100%;
			max-width: 100%;

			min-height: min(50dvh, 256px);
			max-height: min(50dvh, 256px);

			box-sizing: border-box;
			padding: 8px;

			object-fit: contain;
		}
	}
</style>
