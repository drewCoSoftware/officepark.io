// This store is what we use to manage login + permission states.
// It integrates with a MemberMan backend, and so the name of the file/store may change in the future to be more specific.
import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import type { InlineConfig } from 'vitest';
import { fetchy, fetchyPost, type FetchyResponse, type IApiResponse } from 'fetchy';

// NOTE: This was copied from the RelatedSearch project.  We should find a way
// to generalize / share it between apps (NPM?)
let _ApiRoot: string;
let _IsInitialized: boolean = false;

// ----------------------------------------------------------------------
export function InitLoginStore(apiRoot_: string) {
  _ApiRoot = apiRoot_;
  _IsInitialized = true;
}

// ----------------------------------------------------------------------
function ResolveUrl(part: string) {
  if (!_IsInitialized) {
    throw Error("The MemberMan login store is not initialized!  Please call 'InitLoginStore' when your application starts!");
  }
  const res = _ApiRoot + part;
  return res;
}

// // NOTE: This can just be the fetchy result interface.....
// export interface IResult<T> {
//   data?: T | null,
//   success: boolean,
//   message?: string    // Success / error / etc. messages. 
// }

// Describes the current login state for the user.
// Values for DisplayName and Avatar only make sense if the user is logged in.
export interface ILoginState {
  IsLoggedIn: boolean,
  DisplayName?: string,
  Avatar?: string
}

export interface IStoreState {
  LoginState: ILoginState,
  IsBusy: boolean                 // Indicates that the store is busy working on something....
}

export interface SignupResponse extends IApiResponse {
  IsUsernameAvailable: boolean,
  IsEmailAvailable: boolean,
}

export interface LoginResponse extends IApiResponse {
  IsLoggedIn: boolean,
  DisplayName: string,
  Avatar?: string
}

export interface LoginResult {
  LoginOK: boolean,
  ErrorMessage?: string | null    // If there was an error, this is the message that we can display....
}

// The login store is responsible for:
// - Tracking the login state for a single user
// - Being able to add/create/remove/list other users in the system (depending on your permissions).
// - Evaluate permissions for the current user....
export const useLoginStore = defineStore('mmlogin', {

  // TODO: Convert the state type to: IStoreState.
  // We will want to use the busy variable too, and maybe we can even hook it to our forms.
  //https://pinia.vuejs.org/core-concepts/state.html
  state: (): ILoginState => {
    return {
      IsLoggedIn: false,
      DisplayName: '',
      Avatar: ''
    }
  },

  actions: {

    // ---------------------------------------------------
    async VerifyCode(code: string): Promise<FetchyResponse<IApiResponse>> {

      const url = ResolveUrl("/verify");

      let headers: Headers = new Headers();
      headers.append("Content-Type", "application/json");

      const p = fetchyPost(url, {
        Username: "verify-me",
        VerificationCode: code
      }, headers);

      return p;
    },

    // ---------------------------------------------------
    async RequestVerify(username: string): Promise<FetchyResponse<IApiResponse>> {
      const url = ResolveUrl("/reverify");

      let headers: Headers = new Headers();
      headers.append("Content-Type", "application/json");

      let p = await fetchyPost(url, {
        Username: username
      }, headers);
      return p;
    },

    // ---------------------------------------------------
    async CheckLogin() {
      const url = ResolveUrl("/login/validate");
      let p = await fetchy<LoginResponse>(url);
      if (p.Success && p.StatusCode == 200) {
        this.IsLoggedIn = true;
        this.DisplayName = p.Data?.DisplayName,
          this.Avatar = p.Data?.Avatar
      }
      else {
        this.Logout();
      }

    },

    // ---------------------------------------------------
    async Login(username: string, password: string): Promise<FetchyResponse<LoginResponse>> {

      const url = ResolveUrl("/login");
      let p = await fetchy<LoginResponse>(url, {
        method: 'POST',
        body: JSON.stringify({
          username: username,
          password: password
        }),
        headers: { "Content-Type": 'application/json' },
      });

      if (p.Success && p.Data?.IsLoggedIn) {
        // TODO: We might have an OK call, but the user is not logged in?

        // The user is now logged in, so we need to indicate that state here!
        // NOTE: Avatar data can be set here too......
        this.IsLoggedIn = true;
        this.DisplayName = p.Data.DisplayName;
        this.Avatar = p.Data.Avatar;
      }

      if (p.Error) {
        this.Logout();
      }

      return p;
    },

    // ---------------------------------------------------
    async Logout() {
      const url = ResolveUrl("/logout");
      let p = await fetchyPost(url, null);

      // NOTE: We just assume that the call to logout is correct.
      // Server side it will always work.
      // Destroy cookies?
      // https://stackoverflow.com/questions/2144386/how-to-delete-a-cookie

      this.IsLoggedIn = false;
      this.DisplayName = "";
      this.Avatar = "";
    },

    // ---------------------------------------------------
    async RequestVerification(email: string) {
      throw Error("Not implemented!");
    },

    // ---------------------------------------------------
    async ForgotPassword(username: string): Promise<FetchyResponse<IApiResponse>> {
      const url = ResolveUrl("/forgot-password");
      let headers: Headers = new Headers();
      headers.append("Content-Type", "application/json");

      let p = await fetchy<IApiResponse>(url, {
        method: 'POST',
        body: JSON.stringify({ Username: username }),
        headers: headers
      });

      return p;
    },

    // ---------------------------------------------------
    async ResetPassword(resetToken: string, newPassword: string, confirmPassword: string): Promise<FetchyResponse<IApiResponse>> {
      const url = ResolveUrl("/reset-password");
      let headers: Headers = new Headers();
      headers.append("Content-Type", "application/json");

      let p = await fetchy<IApiResponse>(url, {
        method: 'POST',
        body: JSON.stringify({
          ResetToken: resetToken,
          NewPassword: newPassword,
          ConfirmPassword: confirmPassword,
        }),
        headers: headers
      });

      return p;
    },

    // ---------------------------------------------------
    async SignUp(username: string, emailAddr: string, password: string): Promise<FetchyResponse<SignupResponse>> {
      const url = ResolveUrl("/signup");

      let headers: Headers = new Headers();
      headers.append("Content-Type", "application/json");

      // TODO: Use Environment var:
      // headers.append("X-Test-Api-Call", "true");

      let p = await fetchy<SignupResponse>(url, {
        method: 'POST',
        body: JSON.stringify({
          username: username,
          email: emailAddr,
          password: password
        }),
        headers: headers
      });

      return p;
    }

  }

});