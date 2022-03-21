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

interface LoginResponse {
  loginOK: boolean;
  authToken: string;
  authRequired: boolean;
  username: string;
}

export class dtAuthHandler {
  public State: any;

  Login = (username: string, password: string) : Promise<boolean> => {

    // this can serialize form data automatically?
    // const data = new URLSearchParams();
    // for (const pair of new FormData(formElement)) {
    //     data.append(pair[0], pair[1]);
    // }

    // or this:
    // new URLSearchParams(new FormData(formElement))
    

    const data = {
      username : username,
      password: password
    };
    // const data = new FormData();
    // data.append("username", username);
    // data.append("password", password);  

    const p = fetch("https://localhost:7001/api/login", {
      credentials: "include",
      method: "post",
      mode: "cors",

      // headers: {
      //   "content-type": "multipart/form-data"
      // },
      headers: {
        "content-type": "application/json"
      }, 
      // headers: {
      //   "accept": "application/json"
      // },
      // headers: {
      //   "Content-Type": "multipart/form-data"
      // },
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

export class dtAuth {

  private handler = new dtAuthHandler();

  install = (app: App) => {
    this.handler.State = reactive(new dtAuthState());

    app.config.globalProperties.$dta = this.handler;
  }

  public get IsLoggedIn(): boolean {
    return this.handler.State.IsLoggedIn;
  }

}