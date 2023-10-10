import { App, reactive } from 'vue';

// class dtm {
//   IsLoggedIn: false;
// }

// const thingy = {
//   IsLoggedIn: true
// }

class dtAuthHandler {
  private _IsLoggedIn: boolean = false;
  public get IsLoggedIn(): boolean {
    return this._IsLoggedIn;
  }
  public set IsLoggedIn(value:boolean)
  {
    this._IsLoggedIn = value;
  //  alert(value);
  }

  Login = () => {
    // More stuff here, internal state, etc.
   // alert('login');
    this.IsLoggedIn = true;
  }

  Logout = () => {
    // More stuff here, internal state, etc.
    this.IsLoggedIn = false;
  }

}

export default class dtAuth {

  private authState = new dtAuthHandler();
  install = (app: App) => {
    app.config.globalProperties.$dtAuth = reactive(this.authState);
  }

  public get IsLoggedIn(): boolean {
    return this.authState.IsLoggedIn;
  }

//   Login = () => {
//     // NOTE: This is where we would want to bounce against the auth server I think...
//     thingy.IsLoggedIn = true;
// //    this.IsLoggedIn = true;
//    // alert('login');
//   }

//   Logout = () => {
//     thingy.IsLoggedIn = false;
//     this.IsLoggedIn = false;
//    // alert('logout');
//   }
  
}