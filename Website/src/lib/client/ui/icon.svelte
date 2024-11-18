<script lang="ts" module>
  export type IconThickness = 'solid' | 'regular' | 'thin' | 'light'
  export type IconSize = `${number}${'px' | '%' | 'em' | 'rem'}`

  export type IconRotation = 0 | 90 | 180 | 270

  export type IconFlip = 'horizontal' | 'vertical' | 'both'

  export type IconColor =
    | `#${string}`
    | `rgb(${string})`
    | `rgba(${string})`
    | `hsl(${string})`
    | 'inherit'

  export interface IconOptions {
    icon: string
    thickness?: IconThickness
    size?: IconSize
    color?: IconColor
    rotate?: IconRotation
    flip?: IconFlip
    brand?: boolean
  }
</script>

<script lang="ts">
  const {
    icon,
    thickness = 'regular',
    size = '1em',
    color = 'inherit',
    rotate = 0,
    flip,
    brand = false
  }: IconOptions = $props()

  const classes: string[] = []

  classes.push(`fa-${thickness}`, `fa-${icon}`, `fa-${size}`)

  if (rotate !== 0) {
    classes.push(`fa-rotate-${rotate}`)
  }

  if (flip != null) {
    classes.push(`fa-flip${flip}`)
  }

  if (brand) {
    classes.push(`fa-brands`)
  }
</script>

<div
  class="icon"
  style:color
  style:font-size={size}
  style:min-width={size}
  style:max-width={size}
  style:min-height={size}
  style:max-height={size}
>
  <i class="icon {classes.join(' ')}"></i>
</div>

<style lang="scss">
  div.icon {
    display: flex;
    flex-direction: row;
    align-items: center;
    justify-content: center;
  }

  i.icon {
    display: flex;
    flex-direction: column;

    align-items: center;
    justify-content: center;
  }
</style>
