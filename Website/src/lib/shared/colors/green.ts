import { fromRGBHex, type Palette } from '../colors'

const palette: Palette = {
  name: 'Green',
  night: false,

  colors: [
    fromRGBHex(0x0f3031),
    fromRGBHex(0x324f4e),
    fromRGBHex(0x448c79),
    fromRGBHex(0x91a39b),
    fromRGBHex(0xebf4e4),

    // extra
    [255, 128, 128],
    [0x00, 0x00, 0xff],
    [0xff, 0xff, 0x00],
    [0xff, 0xff, 0xff],
    [0x00, 0x00, 0x00],
    [133, 255, 133]
  ]
}
export default palette
