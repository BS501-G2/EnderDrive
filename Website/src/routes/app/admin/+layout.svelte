<script
	lang="ts"
>
	import { createAdminContext } from '$lib/client/contexts/admin';
	import {
		onMount,
		type Snippet
	} from 'svelte';
	import AdminTabHost from './admin-tab-host.svelte';
	import AdminTabs from './admin-tabs.svelte';
	import { useAppContext } from '$lib/client/contexts/app';
	import Title from '../title.svelte';
	import Separator from '$lib/client/ui/separator.svelte';
	import Icon from '$lib/client/ui/icon.svelte';

	const {
		children
	}: {
		children: Snippet;
	} =
		$props();
	const {
		isDesktop,
		isMobile,
		pushTitle
	} =
		useAppContext();

	const {
		tabs,
		sidePanel,
		titleStack
	} =
		createAdminContext();
</script>

<AdminTabs
/>

<div
	class="page"
>
	<div
		class="header"
	>
		<AdminTabHost
			{tabs}
		/>

		{#if $isDesktop}
			<div
				class="title"
			>
				{#if $titleStack.length > 0}
					{#each $titleStack as { id, title } (id)}
						<Title
							{title}
						/>
						<h2
						>
							{title}
						</h2>
					{/each}
				{:else}
					<h2
					>
						Page
					</h2>
				{/if}
			</div>
		{:else if $isMobile}
			{#each $titleStack as { id, title } (id)}
				<Title
					{title}
				/>
			{/each}

			<Separator
				horizontal
			/>
		{/if}
	</div>

	<div
		class="content"
	>
		{#if $isDesktop}
			<div
				class="side"
			>
				{#each $sidePanel as { id, name, icon, snippet }, index (id)}
					{#if index !== 0}
						<Separator
							horizontal
						/>
					{/if}

					<div
						class="entry"
					>
						<div
							class="head"
						>
							<Icon
								{...icon}
							/>
							<h2
							>
								{name}
							</h2>
						</div>
						<div
							class="content"
						>
							{@render snippet()}
						</div>
					</div>
				{/each}
			</div>

			<div
				class="separator"
			></div>
		{/if}
		<div
			class="main"
		>
			{@render children()}
		</div>
	</div>
</div>

<style
	lang="scss"
>
	@use '../../../global.scss'
		as *;

	div.page {
		padding: 8px;

		flex-grow: 1;

		min-height: 0;

		> div.header {
			> div.title {
				background-color: var(
					--color-1
				);
				color: var(
					--color-5
				);

				padding: 16px;

				> h2 {
					font-size: 1.2em;
					font-weight: bolder;
				}
			}
		}

		> div.content {
			flex-direction: row;

			flex-grow: 1;

			min-height: 0;

			> div.side {
				@include force-size(
					256px,
					&
				);

				overflow: hidden
					auto;

				padding: 16px
					8px;
				gap: 16px;

				> div.entry {
					gap: 16px;

					> div.head {
						flex-direction: row;
						align-items: center;

						gap: 8px;
						line-height: 1em;

						> h2 {
							font-size: 1.2em;
							font-weight: bolder;
						}
					}

					> div.content {
					}
				}
			}

			> div.separator {
				@include force-size(
					1px,
					&
				);

				background-color: var(
					--color-1
				);
			}

			> div.main {
				flex-grow: 1;

				overflow: hidden
					auto;
			}
		}
	}
</style>
