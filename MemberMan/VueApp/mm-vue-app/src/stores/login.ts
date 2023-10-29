// This store is what we use to manage login + permission states.
// It integrates with a MemberMan backend, and so the name of the file/store may change in the future to be more specific.


import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import type { InlineConfig } from 'vitest';
import { fetchy, fetchyPost, type FetchyResponse, type IApiResponse } from '@/fetchy';

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

export interface SignupResponse extends IApiResponse {
  IsUsernameAvailable: boolean,
  IsEmailAvailable: boolean,
}

export interface LoginResponse extends IApiResponse {
  IsLoggedIn: boolean,
  DisplayName: string,
  Avatar?: string
}

// The login store is responsible for:
// - Tracking the login state for a single user
// - Being able to add/create/remove/list other users in the system (depending on your permissions).
// - Evaluate permissions for the current user....
export const useLoginStore = defineStore('login', () => {

  // OPTIONS:
  // How long should we keep the current login state in memory?
  const _stateWindow = 5 * 1000 * 60;
  let _CurrentState = {
    IsLoggedIn: false,
    DisplayName: "logged out",
    Avatar: "DefaultLogoutIcon.png"
  };


  // ----------------------------------------------------------------------
  async function RequestVerification(username: string) {

    const url = "https://localhost:7138/api/verify";

    let headers: Headers = new Headers();
    headers.append("Content-Type", "application/json");

    // TODO: Use Environment var:
    // headers.append("X-Test-Api-Call", "true");

    let p = fetchyPost(url, { username: username, x:124 }, headers);
    return p;
  }

  // ----------------------------------------------------------------------
  async function SignUp(username: string, emailAddr: string, password: string): Promise<FetchyResponse<SignupResponse>> {
    const url = "https://localhost:7138/api/signup";

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

  // ----------------------------------------------------------------------
  async function Login(username: string, password: string): Promise<FetchyResponse<LoginResponse>> {

    const url = "https://localhost:7138/api/login";
    let p = await fetchy<LoginResponse>(url, {
      method: 'POST',
      body: JSON.stringify({
        username: username,
        password: password
      }),
      headers: { "Content-Type": 'application/json' }
    });

    return p;
  }

  // ----------------------------------------------------------------------
  function GetState(refresh: boolean = false): ILoginState {
    // TODO: Fetch the current login state for the user from the server.
    // in SPA applications, this may need to be done for each API call / route.
    return {
      IsLoggedIn: false,
      DisplayName: "invalid",
      Avatar: "invalid"
    }
  }

  // ----------------------------------------------------------------------
  // Logs out the current user.  This can also be used to force the logout of the user
  // from a different component.
  function Logout(force: boolean = false) {
    throw Error("Not implemented!");
  }

  return { GetState, Login, Logout, SignUp, RequestVerification };
});