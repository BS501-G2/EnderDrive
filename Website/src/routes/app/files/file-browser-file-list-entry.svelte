<script
	lang="ts"
>
	import {
		FileType,
		useServerContext
	} from '$lib/client/client';
	import { useAppContext } from '$lib/client/contexts/app';
	import {
		useFileBrowserContext,
		type FileEntry
	} from '$lib/client/contexts/file-browser';
	import { useFileBrowserListContext } from '$lib/client/contexts/file-browser-list';
	import UserLink from '$lib/client/model/user-link.svelte';
	import Button from '$lib/client/ui/button.svelte';
	import Icon from '$lib/client/ui/icon.svelte';
	import LoadingSpinner from '$lib/client/ui/loading-spinner.svelte';
	import {
		onMount,
		type Snippet
	} from 'svelte';
	import { get } from 'svelte/store';
	import { fly } from 'svelte/transition';

	const {
		file
	}: {
		file: FileEntry;
	} =
		$props();
	const {
		pushFile,
		selectedFileIds,
		selectFile,
		deselectFile
	} =
		useFileBrowserListContext();
	const {
		getFileContents,
		getFileMime,
		getFileSnapshots,
		getMainFileContent,
		getUser,
		getFileSize
	} =
		useServerContext();
	const {
		isMobile,
		isDesktop
	} =
		useAppContext();
	const {
		onFileId
	} =
		useFileBrowserContext();

	let fileElement: HTMLElement =
		$state(
			null as never
		);
	let hover: boolean =
		$state(
			false
		);

	onMount(
		() =>
			pushFile(
				file,
				fileElement
			)
	);

	const getModified =
		async () => {
			const fileSnapshot =
				(
					await getFileSnapshots(
						file
							.file
							.id,
						void 0,
						0,
						1
					)
				)[0];
			const user =
				await getUser(
					fileSnapshot.authorUserId
				);

			return [
				fileSnapshot,
				user!
			] as const;
		};

	const getSize =
		async () => {
			let fileSize =
				await getFileSize(
					file
						.file
						.id
				);

			let count = 0;
			const dict =
				[
					'',
					'K',
					'M',
					'G',
					'T'
				] as const;

			while (
				fileSize >=
				1000
			) {
				fileSize /= 1024;
				count++;
			}

			return `${fileSize.toFixed(2)} ${dict[count]}iB`;
		};
</script>

<!-- svelte-ignore a11y_interactive_supports_focus -->
<!-- svelte-ignore a11y_no_static_element_interactions -->
<div
	bind:this={fileElement}
	oncontextmenu={(
		event
	) => {
		if (
			get(
				isMobile
			)
		) {
			event.preventDefault();

			$selectedFileIds.includes(
				file
					.file
					.id
			)
				? deselectFile(
						file
							.file
							.id
					)
				: selectFile(
						file
							.file
							.id
					);
		}
	}}
	onclick={(
		event
	) => {
		if (
			get(
				isMobile
			) &&
			$selectedFileIds.length
		) {
			event.preventDefault();

			$selectedFileIds.includes(
				file
					.file
					.id
			)
				? deselectFile(
						file
							.file
							.id
					)
				: selectFile(
						file
							.file
							.id
					);
		}
	}}
	class="file"
	class:mobile={$isMobile}
	onmouseenter={() => {
		hover = true;
	}}
	onmouseleave={() => {
		hover = false;
	}}
	onkeypress={() => {}}
>
	{#if $isDesktop || ($isMobile && $selectedFileIds.length)}
		<div
			class="check"
			transition:fly={{
				x: -16,
				duration: 150
			}}
		>
			{#if hover || $selectedFileIds.includes(file.file.id)}
				<button
					class="check"
					onclick={() => {
						$selectedFileIds.includes(
							file
								.file
								.id
						)
							? deselectFile(
									file
										.file
										.id
								)
							: selectFile(
									file
										.file
										.id
								);
					}}
				>
					{#if $selectedFileIds.includes(file.file.id)}
						<Icon
							icon="circle-check"
							size="18px"
						/>
					{:else}
						<Icon
							icon="circle"
							size="18px"
						/>
					{/if}
				</button>
			{/if}
		</div>
	{/if}

	<div
		class="preview"
	>
		{#if file.type === 'folder'}
			<Icon
				icon="folder"
				size="32px"
			/>
		{:else}
			{#await getFileMime(file.file.id)}
				<LoadingSpinner
					size="32px"
				/>
			{:then mime}
				{#if mime.startsWith('image/')}
					<Icon
						icon="image"
						size="32px"
					/>
				{:else if mime.startsWith('video/')}
					<Icon
						icon="film"
						size="32px"
						thickness="solid"
					/>
				{:else if mime.startsWith('audio/')}
					<Icon
						icon="music"
						size="32px"
					/>
				{:else if mime.startsWith('text/')}
					<Icon
						icon="text"
						size="32px"
					/>
				{:else if mime.startsWith('application/')}
					<Icon
						icon="file"
						size="32px"
					/>
				{:else}
					<Icon
						icon="file"
						size="32px"
					/>
				{/if}
			{/await}
		{/if}
	</div>

	<div
		class="name"
	>
		<div
			class="file-name"
		>
			<a
				href="/app/files?fileId={file
					.file
					.id}"
				ondblclick={(
					event
				) => {
					onFileId?.(
						event as never,
						file
							.file
							.id
					);
				}}
				onclick={(
					event
				) => {
					event.preventDefault();

					if (
						$isMobile
					) {
						onFileId?.(
							event as never,
							file
								.file
								.id
						);
					} else {
						$selectedFileIds =
							[
								file
									.file
									.id
							];
					}
				}}
				class:mobile={$isMobile}
			>
				<p
					class="name"
				>
					{file
						.file
						.name}
				</p>

				{#if $isMobile}
					<p
						class="size"
					>
						{#await getSize()}
							<LoadingSpinner
								size="1em"
							/>
						{:then size}
							{size}
						{/await}
					</p>
				{/if}
			</a>
		</div>
	</div>

	{#if $isDesktop}
		<div
			class="size"
		>
			{#await getSize()}
				<LoadingSpinner
					size="1em"
				/>
			{:then size}
				<p
				>
					{size}
				</p>
			{/await}
		</div>
	{/if}

	{#if $isDesktop}
		<div
			class="modified"
		>
			{#if file.file.type === FileType.File}
				{#await getModified()}
					<LoadingSpinner
						size="1em"
					/>
				{:then [fileSnapshot, user]}
					<p
						class="user"
					>
						{new Date(
							fileSnapshot.createTime
						).toLocaleString()}
						by
						<UserLink
							userId={user.id}
						/>
					</p>
				{/await}
			{/if}
		</div>

		<div
			class="date"
		></div>
	{/if}
</div>

<style
	lang="scss"
>
	@use '../../../global.scss'
		as *;

	div.file {
		color: var(
			--color-1
		);

		flex-direction: row;
		align-items: center;

		overflow: hidden;

		padding: 8px;
		gap: 8px;

		> div.check {
			@include force-size(
				32px,
				32px
			);

			align-items: center;
			justify-content: center;

			> button {
				border: none;
				text-decoration: none;

				cursor: pointer;
			}
		}

		> div.name {
			flex-grow: 1;
			min-width: 172px;

			flex-direction: row;
			align-items: center;

			> div.file-name {
				flex-direction: column;
				flex-grow: 1;

				min-width: 0;

				> a {
					text-decoration: none;

					color: inherit;

					min-width: 0;

					> p.name {
						text-overflow: ellipsis;
						text-wrap: nowrap;

						overflow: hidden;

						line-height: 1.2em;
					}

					> p.size {
						font-size: 0.8em;
					}
				}

				> a.mobile {
					flex-grow: 1;
				}
			}

			> div.actions {
				flex-direction: row;

				div.foreground {
					padding: 8px;
				}
			}
		}

		> div.modified {
			flex-shrink: 0;

			@include force-size(
				256px,
				&
			);
		}

		> div.size {
			@include force-size(
				96px,
				&
			);
		}
	}

	div.file.mobile {
		padding: 8px;
	}

	div.file:hover {
		background-color: var(
			--color-5
		);

		> div.name {
			> div.file-name {
				> a:not(
						.mobile
					) {
					text-decoration: underline;
				}
			}
		}
	}
</style>
