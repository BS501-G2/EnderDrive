export function* range(count: number): Generator<number> {
	for (let index = 0; index < count; index++) {
		yield index;
	}
}
