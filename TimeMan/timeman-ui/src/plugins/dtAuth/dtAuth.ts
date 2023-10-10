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
export interface SignupResponse {
  availability: MemberAvailability;
  message: string;
}

// ==============================================================================================
export interface MemberAvailability {
  isUsernameAvailable: boolean;
  isEmailAvailable: boolean;
}


// ==============================================================================================
export class dtAuthHandler {
  public State: any;

  private _loginEndPoint: string;
  private _signupEndpoint: string;

  constructor(loginEndpoint: string, signupEndpoint: string) {
    this._loginEndPoint = loginEndpoint;
    this._signupEndpoint = signupEndpoint;
  }

  Signup = (username: string, email: string, password: string): Promise<SignupResponse> => {
    const data = {
      username: username,
      email: email,
      password: password
    };

    const p = fetch(this._signupEndpoint,
      {
        credentials: "include",
        method: "post",
        mode: "cors",
        headers: {
          "content-type": "application/json"
        },
        body: JSON.stringify(data)
      });

    const res = p.then((response) => response.json())
      .catch(error => {
        throw (error);
      })
      .then<SignupResponse>((data: SignupResponse) => {
        return data;
      });

    return res;
  }

  Login = (username: string, password: string): Promise<boolean> => {

    const data = {
      username: username,
      password: password
    };

    // NOTE: This block of code could easily be wrapped up into a single function for 
    // POSTing JSON data via cors/no-cors.
    const p = fetch(this._loginEndPoint, {
      credentials: "include",
      method: "post",
      mode: "cors",
      headers: {
        "content-type": "application/json"
      },
      body: JSON.stringify(data)
    });

    const res = p.then((response) => response.json())
      .catch(error => {
        throw (error);
      })
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

  constructor(loginEndpoint: string, signupEndpoint: string) {
    this.handler = new dtAuthHandler(loginEndpoint, signupEndpoint);
  }

  install = (app: App) => {
    this.handler.State = reactive(new dtAuthState());

    app.config.globalProperties.$dtAuth = this.handler;
  }

  public get IsLoggedIn(): boolean {
    return this.handler.State.IsLoggedIn;
  }

}

// Putting the type declarations here does the trick.. Not sure why the d.ts file in the same folder
// is being ignored.....
declare module '@vue/runtime-core' {
  export interface ComponentCustomProperties {
    $dtAuth: dtAuthHandler
  }
}