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
        <input v-model="password" type="text" />
      </div>
      <div>
        <button v-on:click="loginUser">Login</button>
      </div>
    </div>
  </div>
</template>

<script lang="ts">
import { Options, Vue } from "vue-class-component";

@Options({
  // data() {
  //   return {
  //     isLoggingIn: false
  //   }
  // }
})
export default class Login extends Vue {
  // data() {
  // },

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
      .then((data) => {
//        alert("the login happened!");
        console.dir(data);
      })
      .finally(() => {
//        alert('then this');
        this.endLogin();
      });

    // alert(this.username + " " + this.password);
    // This is where we call some kind of login/membership service....
    // Let's just fake it with a long delay for now... maybe a dummmy call to some HTTP service?
  }

  beginLogin() {
    this.isLoggingIn = true;
  }
  endLogin() {
//    alert('login is over!');
    this.isLoggingIn = false;
  }
}
</script>

<style lang="less">
  .login-form {
    background:green;
  }
  .login-form.active{
    background:red;
  }
</style>