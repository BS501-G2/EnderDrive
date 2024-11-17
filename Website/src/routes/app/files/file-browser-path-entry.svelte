<script
	lang="ts"
>
	import {
		FileType,
		type FileResource
	} from '$lib/client/client';
	import Icon from '$lib/client/ui/icon.svelte';
	import FileBrowserPathMenu from './file-browser-path-menu.svelte';

	const {
		file
	}: {
		file: FileResource;
	} =
		$props();

	let button: HTMLButtonElement =
		$state(
			null as never
		);
	let show: boolean =
		$state(
			false
		);
</script>

<div
	class="file-entry"
>
	<div
		class="arrow"
	>
		<Icon
			icon="chevron-right"
			thickness="solid"
			size="0.5em"
		/>
	</div>

	<button
		bind:this={button}
		class="file"
		onclick={() => {
			show = true;
		}}
	>
		{#if file.type === FileType.Folder}
			<Icon
				icon="folder"
			/>
		{:else}
			<Icon
				icon="file"
			/>
		{/if}

		<p
			class="name"
			title={file.name}
		>
			{file.name}
		</p>
	</button>
</div>

{#if show}
	<FileBrowserPathMenu
		{file}
		{button}
		ondismiss={() => {
			show = false;
		}}
	/>
{/if}

<style
	lang="scss"
>
	div.file-entry {
		flex-direction: row;

		flex-shrink: 0;
		min-width: 0;

		> div.arrow {
			flex-direction: row;

			align-items: center;
		}

		> button.file {
			overflow: hidden
				hidden;

			display: flex;
			flex-direction: row;
			align-items: center;
			line-height: 1em;

			border: none;
			cursor: pointer;

			padding: 4px;
			margin: 4px;
			gap: 8px;

			min-width: 0;
			flex-shrink: 0;

			> p.name {
				overflow: hidden
					hidden;
				text-overflow: ellipsis;
				text-wrap: nowrap;

				min-width: 0;

				max-width: 128px;
				flex-shrink: 0;
			}
		}

		> button.file:hover {
			background-color: #0000004f;
		}
	}
</style>
