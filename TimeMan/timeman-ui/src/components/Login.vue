<template>
  <div>
    <h2>Login</h2>
    <div
      class="form login-form"
      :class="{ active: isLoggingIn, 'has-error': loginError }"
    >
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
  isLoginOK: boolean = false;

  loginUser() {
    this.beginLogin();

    this.$dta.Login("123", "abc").then((loginOK: boolean) => {
      if (loginOK) {
        // We want to set the token and do any redirects
        // to the appropriate page here......
        let to = this.$route.query["to"]?.toString();
        if (to == null) {
          to = "/";
        }
        this.$router.push(to);
      } else {
        // This is where we can set some stuff on the UI
        // to indicate that there was a bad name or password.
        alert("bad name or password!");
      }
    });

    // this.$dta.Login(this.username, this.password)
    //   .then((loginOK) => {
    //     if (loginOK) {
    //       // We want to set the token and do any redirects
    //       // to the appropriate page here......
    //       let to = this.$route.query["to"]?.toString();
    //       if (to == null) {
    //         to = "/";
    //       }
    //       this.$router.push(to);
    //     } else {
    //       // This is where we can set some stuff on the UI
    //       // to indicate that there was a bad name or password.
    //       alert("bad name or password!");
    //     }
    //   })
    //   .finally(() => {
    //     this.endLogin();
    //   });

    // let p = fetch("https://localhost:7001/api/login", {
    //   credentials: "include",
    //   method: "post",
    // });
    // p.then((response) => response.json())
    //   .then((data: LoginResponse) => {
    //     if (data.loginOK) {

    //       this.$dta.Login(data.username, data.authToken);

    //       // We want to set the token and do any redirects
    //       // to the appropriate page here......
    //       let to = this.$route.query["to"]?.toString();
    //       if (to == null) {
    //         to = "/";
    //       }
    //       this.$router.push(to);
    //     } else {
    //       // This is where we can set some stuff on the UI
    //       // to indicate that there was a bad name or password.
    //       alert("bad name or password!");
    //     }
    //   })
    //   .finally(() => {
    //     this.endLogin();
    //   });
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
.login-form.has-error {
  //  background: green;
  border: solid 1px red;
}

.login-form.active {
  background: red;
}
</style>