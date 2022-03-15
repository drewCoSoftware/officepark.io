<template>
  <div>
    <h2>Login</h2>
    <div class="form login-form">
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

@Options({})
export default class Login extends Vue {
  username = "abc";
  password: string = "";

  loginUser() {
    // alert(this.username + " " + this.password);
    // This is where we call some kind of login/membership service....
    // Let's just fake it with a long delay for now... maybe a dummmy call to some HTTP service?
  }

  // data() {
  //   return {
  //     username: "",
  //     password: ""
  //   };
  // }
}
</script>