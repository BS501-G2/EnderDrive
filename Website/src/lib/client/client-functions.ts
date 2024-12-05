/* eslint-disable @typescript-eslint/no-empty-object-type */
/* eslint-disable @typescript-eslint/no-explicit-any */

import type { RemoteFunction, TranslatorMap } from './client'

export interface ClientSideFunctions extends Record<string, RemoteFunction<any, any, any, any>> {
  Notify: RemoteFunction<
    {
      NotificationId: string
    },
    object
  >

  IsHandlerAvailable: RemoteFunction<{ Name: string }, { IsAvailable: boolean }>
}

export const responseTranslators: TranslatorMap<ClientSideFunctions> = {
  Notify: [(data) => data, (data) => data],
  IsHandlerAvailable: [(data) => data, (data) => data]
}
