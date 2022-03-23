<template>
  <div>
    <h2>Login</h2>
    <div
      class="form login-form"
      :class="{ active: isLoggingIn, 'has-error': loginError }"
    >
      <p class="form-msg">{{ formMsg }}</p>
      <div>
        <label for="username">username</label>
        <input v-model="username" type="text" />
      </div>
      <div>
        <label for="password">password</label>
        <input v-model="password" type="password" />
      </div>
      <div>
        <button v-on:click="loginUser" :disabled="isLoggingIn">Login</button>
      </div>
    </div>
  </div>
</template>

<script lang="ts">
import { Options, Vue } from "vue-class-component";

@Options({})
export default class Login extends Vue {
  username = "abc";
  password: string = "";

  isLoggingIn: boolean = false;
  loginError: boolean = false;
  isLoginOK: boolean = true;

  formMsg = "";

  loginUser() {
    this.beginLogin();

    this.$dtAuth
      .Login(this.username, this.password)
      .then((loginOK: boolean) => {
        if (loginOK) {
          // We want to set the token and do any redirects
          // to the appropriate page here......
          this.isLoginOK = true;

          let to = this.$route.query["to"]?.toString();
          if (to == null) {
            to = "/";
          }
          this.$router.push(to);
        } else {
          // This is where we can set some stuff on the UI
          // to indicate that there was a bad name or password.
          this.formMsg = "Invalid username or password!";
          this.password = "";
          this.isLoginOK = false;
          this.loginError = true;
        }
      })
      .catch((error) => {
        this.setError("There was a problem contacting the login server!");

        // rethrow:
        throw error;
      })
      .finally(() => {
        this.endLogin();
      });
  }

  beginLogin() {
    this.isLoggingIn = true;
  }
  endLogin() {
    this.isLoggingIn = false;
  }

  private setError = (msg:string) => {
    this.isLoginOK = false;
    this.password = "";
    this.formMsg = msg;
    this.loginError = true;

  }

  private clearError()
  {
      this.isLoginOK = true;
      this.formMsg = "";
  }
}
</script>

<style lang="less">
.login-form {
  .form-msg {
    display: none;
    color: red;
  }
}
.login-form.has-error {
  border: solid 1px red;

  .form-msg {
    display: block;
  }
}

.login-form.active {
  background: red;
}
</style>