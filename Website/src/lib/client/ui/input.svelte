<script
	lang="ts"
	module
>
	import {
		writable,
		type Writable
	} from 'svelte/store';
	import type { IconOptions } from './icon.svelte';

	export type InputClass =

			| 'primary'
			| 'primary-container'
			| 'background'
			| 'transparent';

	export type InputType =

			| 'text'
			| 'password'
			| 'number';

	export type InputOptions<
		T extends
			InputType
	> =
		({
			class?: InputClass;
			type: InputType;

			icon?: IconOptions;
			name?: string;

			value: unknown;
			tabIndex?: number;
		} & (
			| {
					type:
						| 'text'
						| 'password';
					placeholder?: string;
					value: string;
					update?: (
						value: string
					) => void;
			  }
			| {
					type: 'number';
					placeholder?: string;
					value: number;
					update?: (
						value: number
					) => void;
			  }
		)) & {
			type: T;
		};
</script>

<script
	lang="ts"
	generics="T extends InputType"
>
	import Icon from './icon.svelte';

	let {
		id,
		value = $bindable(
			''
		),
		focus = $bindable(),
		focused = $bindable(
			true
		),
		error,
		onsubmit:
			onSubmit,
		...options
	}: InputOptions<T> & {
		id: string;
		focus?: () => void;
		focused?: boolean;
		error?: Error;
		onsubmit?: (
			event: Event
		) => void;
	} = $props();

	// const icon = options.icon ?? 'fa-regular fa-pen-to-square';
	const icon: IconOptions =
		options.icon ?? {
			thickness:
				'regular',
			icon: 'pen-to-square'
		};
	const inputClass =
		options.class ??
		'primary';

	let inputElement: Writable<HTMLInputElement | null> =
		writable(
			null
		);

	$effect(
		() =>
			(focus =
				() => {
					$inputElement?.focus();
				})
	);
</script>

<button
	class="input {inputClass}"
	onclick={() =>
		void $inputElement?.focus()}
	onfocus={() =>
		void $inputElement?.select()}
>
	<Icon
		size="16px"
		{...icon}
	/>

	<div
		class="field"
	>
		<p
			class="label"
		>
			{#if options.name != null}
				{options.name}
			{/if}
			{#if error != null}
				<span
					>{error.message}</span
				>
			{/if}
		</p>
		<div
			class="input"
		>
			<form
				onsubmit={(
					event
				) => {
					event.preventDefault();
					onSubmit?.(
						event
					);
				}}
			>
				{#if options.type === 'text' || options.type === 'password'}
					{@const {
						placeholder
					} =
						options}
					<input
						{id}
						name={options.name}
						bind:this={$inputElement}
						type={options.type ===
						'password'
							? 'password'
							: 'text'}
						{placeholder}
						bind:value
					/>
				{:else if options.type === 'number'}
					{@const {
						placeholder
					} =
						options}
					<input
						bind:this={$inputElement}
						type="number"
						{placeholder}
						bind:value
					/>
				{/if}
			</form>
		</div>
	</div>
</button>

<style
	lang="scss"
>
	form {
		display: contents;
	}

	button.input {
		cursor: text;

		max-width: 100%;
		min-width: 100%;

		display: flex;
		flex-direction: row;
		align-items: center;

		border: solid
			1px
			transparent;

		font-size: inherit;
		font-weight: inherit;

		padding: 4px
			8px;
		gap: 8px;

		> div.field {
			min-width: 0px;
			flex-grow: 1;

			display: flex;
			flex-direction: column;
			align-items: safe
				start;
			overflow: hidden;

			> p.label {
				min-width: 100%;
				max-width: 100%;
				font-size: 0.75em;
				text-align: start;

				overflow: hidden;
				text-overflow: ellipsis;
				text-wrap: nowrap;
			}

			> div.input {
				min-width: 100%;
				max-width: 100%;
				flex-grow: 1;

				> form {
					> input {
						min-width: 100%;
						max-width: 100%;
						min-height: 100%;
						box-sizing: border-box;

						background-color: inherit;
						color: inherit;

						outline: none;
						border: none;
						border-bottom: solid
							1px
							transparent;

						font-size: inherit;
						font-weight: inherit;
					}

					> input:focus {
						border-bottom-color: var(
							--onBackground
						);
					}

					> input:autofill {
						background-color: inherit;
					}
				}
			}
		}
	}

	button.input {
		background-color: var(
			--backgroundVariant
		);
		color: var(
			--onBackgroundVariant
		);
		border-color: var(
			--shadow
		);

		transition: all
			linear
			150ms;
	}

	button.input:hover {
		border-color: var(
			--onBackgroundVariant
		);
	}

	button.input:focus-within {
		background-color: var(
			--background
		);
		color: var(
			--onBackground
		);
		border-color: var(
			--onBackground
		);
	}
</style>
