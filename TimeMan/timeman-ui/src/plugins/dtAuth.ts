import { App, reactive } from 'vue';

class dtAuthState {
  private _IsLoggedIn: boolean = false;
  public get IsLoggedIn(): boolean {
    return this._IsLoggedIn;
  }
  public set IsLoggedIn(value: boolean) {
    this._IsLoggedIn = value;
  }

  private _UserName: string = "";
  public get UserName(): string {
    return this._UserName;
  }
  public set UserName(value: string) {
    this._UserName = value;
  }

  private _AuthToken: string = "";
  public get AuthToken(): string {
    return this._AuthToken;
  }
  public set AuthToken(value: string) {
    this._AuthToken = value;
  }

}

export class dtAuthHandler {
  public State: any;

  // private _UserName: string = "";
  // private _UserToken: string = "";
  // public get UserName(): string { return this._UserName; }
  // public get UserToken(): string { return this._UserToken; }

  Login = (username: string, usertoken: string) => {
    // NOTE: This is where we would want to bounce against the auth server I think...
    // this._UserName = username;
    // this._UserToken = usertoken;

    this.State.UserName = username;
    this.State.UserToken = usertoken;
    this.State.IsLoggedIn = true;
  }

  Logout = () => {
    this.State.UserName = "null";
    this.State.AuthToken = "";
    this.State.IsLoggedIn = false;
  }

}

export class dtAuth {

  //  private authState = reactive(new dtAuthHandler());
  private handler = new dtAuthHandler();

  install = (app: App) => {
    this.handler.State = reactive(new dtAuthState());

    app.config.globalProperties.$dta = this.handler;
  }

  public get IsLoggedIn(): boolean {
    return this.handler.State.IsLoggedIn;
  }

}