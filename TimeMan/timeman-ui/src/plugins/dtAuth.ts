import { App, reactive } from 'vue';

// ==============================================================================================
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

// ==============================================================================================
interface LoginResponse {
  loginOK: boolean;
  authToken: string;
  authRequired: boolean;
  username: string;
}

// ==============================================================================================
export class dtAuthHandler {
  public State: any;

  private _endPoint: string;

  constructor(endpoint: string) {
    this._endPoint = endpoint;
  }

  Login = (username: string, password: string): Promise<boolean> => {

    const data = {
      username: username,
      password: password
    };

    // NOTE: This block of code could easily be wrapped up into a single function for 
    // POSTing JSON data via cors/no-cors.
    const p = fetch(this._endPoint, {
      credentials: "include",
      method: "post",
      mode: "cors",
      headers: {
        "content-type": "application/json"
      },
      body: JSON.stringify(data)
    });

    const res = p.then((response) => response.json())
      .then<boolean>((data) => {
        if (data.loginOK) {
          this.State.IsLoggedIn = true;
          this.State.AuthToken = data.authToken;
          this.State.UserName = data.username;

          return true;
        }
        else {
          this.Logout();
          return false;
        }
      });

    return res;
  }

  Logout = () => {
    this.State.UserName = "null";
    this.State.AuthToken = "";
    this.State.IsLoggedIn = false;
  }

}

// ==============================================================================================
export class dtAuth {

  private handler: dtAuthHandler;

  constructor(endpoint: string) {
    this.handler = new dtAuthHandler(endpoint);
  }

  install = (app: App) => {
    this.handler.State = reactive(new dtAuthState());

    app.config.globalProperties.$dtAuth = this.handler;
  }

  public get IsLoggedIn(): boolean {
    return this.handler.State.IsLoggedIn;
  }

}