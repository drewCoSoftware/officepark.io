import { App } from 'vue';

export default class {
  
  private _IsLoggedIn: boolean = false;
  public get IsLoggedIn(): boolean {
    return this._IsLoggedIn;
  }

  install = (app: App) => {
    app.config.globalProperties.$dtAuth = this;
  }

  Login = () => {
    // NOTE: This is where we would want to bounce against the auth server I think...
    this._IsLoggedIn = true;
  }

  Logout = () => {
    this._IsLoggedIn = false;
  }
  
}