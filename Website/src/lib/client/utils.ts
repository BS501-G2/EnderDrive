const byteUnitDictionary = Object.freeze(['', 'Ki', 'Mi', 'Gi', 'Ti', 'Pi'] as const)
export function toReadableSize(bytes: number) {
  let count = 0

  while (bytes >= 1000) {
    bytes /= 1024
    count++
  }

  return `${Math.round(bytes * 100) / 100} ${byteUnitDictionary[count]}B`
}
