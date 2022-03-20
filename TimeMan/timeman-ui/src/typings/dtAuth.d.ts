import dtAuthHandler from '../plugins/dtAuth';

declare module '@vue/runtime-core' {
  export interface ComponentCustomProperties {
    $dta: dtAuthHandler
  }
}