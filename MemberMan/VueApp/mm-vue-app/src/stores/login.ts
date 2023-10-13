// This store is what we use to manage login + permission states.
// It integrates with a MemberMan backend, and so the name of the file/store may change in the future to be more specific.


import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import type { InlineConfig } from 'vitest';

// Describes the current login state for the user.
// Values for DisplayName and Avatar only make sense if the user is logged in.
export interface ILoginState {
  IsLoggedIn: boolean,
  DisplayName?: string,
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
  const _CurrentState = ref<ILoginState>({
    IsLoggedIn:false,
    DisplayName:"logged out",
    Avatar:"DefaultLogoutIcon.png"
  });

  // ----------------------------------------------------------------------
  function Login(username: string, password: string): [boolean, string] {
    _CurrentState.value = {
      IsLoggedIn: true,
      DisplayName: "state-test",
      Avatar: "some-icon.jpg"
    };

    // Just assume that everything is good to go!
    return [
      false,
      "LoginOK!"      // NOTE: The string is where the error message would go!
    ];
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
  function Logout(force:boolean = false) {
    throw Error("Not implemented!");
  }

  return { GetState, LoginUser: Login, Logout, _CurrentState};
});