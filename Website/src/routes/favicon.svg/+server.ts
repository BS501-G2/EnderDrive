import Favicon from '../../lib/client/ui/favicon.svelte';
import { render } from 'svelte/server';
import type { RequestEvent } from './$types';

export function GET({
	url
}: RequestEvent) {
	const size =
		Number(
			url.searchParams.get(
				'size'
			) ??
				16
		);
	if (
		Number.isNaN(
			size
		)
	) {
		return new Response(
			null,
			{
				status: 400
			}
		);
	}

	const html = `${
		render(
			Favicon,
			{
				props:
					{
						size: size as never
					}
			}
		)
			.body
	}`;

	const headers =
		new Headers();

	headers.set(
		'content-type',
		'image/svg+xml'
	);
	headers.set(
		'content-length',
		`${html.length}`
	);

	const response =
		new Response(
			html,
			{
				headers
			}
		);
	return response;
}
