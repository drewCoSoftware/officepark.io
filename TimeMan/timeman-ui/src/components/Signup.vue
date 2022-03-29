<template>
  <div>
    <h2>Sign Up</h2>
    <div
      class="form signup-form"
      :class="{ active: isActive, 'has-error': hasError }"
    >
      <p class="form-msg">{{ formMsg }}</p>
      <div>
        <label for="username">username</label>
        <input v-model="username" type="text" />
      </div>
      <div>
        <label for="email">email</label>
        <input v-model="email" type="email" />
      </div>
      <div>
        <label for="password">password</label>
        <input v-model="password" type="password" />
      </div>
      <div>
        <button v-on:click="signupUser" :disabled="isActive">Sign Up</button>
      </div>
    </div>
  </div>
</template>

<script lang="ts">
import { Options, Vue } from "vue-class-component";
import { SignupResponse } from "../plugins/dtAuth/dtAuth"

@Options({})
export default class Signup extends Vue {
  username: string = "";
  email: string = "";
  password: string = "";
  formMsg = "";

  isActive: boolean = false;
  hasError: boolean = false;

  signupUser() {
    this.beginSubmit();


    let p = this.$dtAuth.Signup(this.username, this.email, this.password);
    p.then((signup:SignupResponse) => {

        alert('we got a response!');
        console.dir(signup);
    });


    this.endSubmit();
  }

  beginSubmit() {
    this.isActive = true;
  }

  endSubmit() {
    this.isActive = false;
  }
}
</script>
