import { dtAuthHandler } from './dtAuth';

declare module '@vue/runtime-core' {
  export interface ComponentCustomProperties {
    $dtAuth: dtAuthHandler
  }
}