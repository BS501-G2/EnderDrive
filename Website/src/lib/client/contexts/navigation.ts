import {
	getContext,
	setContext,
	type Snippet
} from 'svelte';
import {
	writable,
	type Writable
} from 'svelte/store';

const navigationContextName =
	'Navigation Context';

export interface NavigationEntry {
	id: number;

	snippet: Snippet;
}

export interface NavigationPage {
	id: string;
}

export function useNavigationContext() {
	return getContext<
		ReturnType<
			typeof createNavigationContext
		>['context']
	>(
		navigationContextName
	);
}

export function createNavigationContext() {
	const navigationEntries: Writable<
		NavigationEntry[]
	> =
		writable(
			[]
		);

	const context =
		setContext(
			navigationContextName,
			{
				pushNavigation:
					(
						snippet: Snippet
					) => {
						const id =
							Math.random();

						navigationEntries.update(
							(
								value
							) => [
								...value,
								{
									id,
									snippet
								}
							]
						);

						return () =>
							navigationEntries.update(
								(
									value
								) =>
									value.filter(
										(
											value
										) =>
											value.id !==
											id
									)
							);
					}
			}
		);

	return {
		context,
		navigationEntries
	};
}
