<template>
  <div>
    <h2>Login</h2>
    <div class="form login-form" :class="{ active: isLoggingIn }">
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

interface LoginResponse {
  LoginOK: boolean;
  AuthToken: string;
  AuthRequired: boolean;
}

@Options({})
export default class Login extends Vue {
  username = "abc";
  password: string = "";

  isLoggingIn: boolean = false;

  loginUser() {
    this.beginLogin();

    let p = fetch("https://localhost:7001/api/login", {
      credentials: "include",
      method: "post",
    });
    p.then((response) => response.json())
      .then((data: LoginResponse) => {
        console.dir(data);
        if (data.LoginOK) {
          // We want to set the token and do any redirects
          // to the appropriate page here......
//          this.$dtAuth.
          this.$router.push("/");
        } else {
          // This is where we can set some stuff on the UI
          // to indicate that there was a bad name or password.
          alert("bad name or password!");
        }
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
}
</script>

<style lang="less">
.login-form {
  background: green;
}
.login-form.active {
  background: red;
}
</style>