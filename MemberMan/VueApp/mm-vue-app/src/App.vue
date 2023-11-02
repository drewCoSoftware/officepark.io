<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
import HelloWorld from './components/HelloWorld.vue'
import type { IStatusData } from "./fetchy.js";
import { useLoginStore } from './stores/login';
import type { ILoginState } from "./stores/login";
import { ref } from 'vue';

// For each page, or every so often we want to update the login status...
// NOTE: The back-end of the application is responsible for evauluating the
// permissions of the current user.
const _Login = useLoginStore();
const loginState = ref(_Login.GetState());

</script>



<template>
  <header class="login-header">
    <div v-if="loginState?.IsLoggedIn">
      <img :src="loginState.Avatar" alt="avatar image" />
      <p>{{ loginState.DisplayName }}</p>
      <button class="link" @click="_Login.Logout">Log Out</button>
    </div>
    <div v-else>
      <p>Not Logged In</p>
    </div>
  </header>

  <main>
    <div class="title">
      <h1>Member Man Tester</h1>
      <nav>
        <RouterLink to="/">Home</RouterLink>
        <RouterLink to="/register">Register</RouterLink>
      </nav>
    </div>

    <RouterView />


    <div class="todo">
      <h2>TODO</h2>

      <h3>Back End</h3>
      <h3>Front-end</h3>
      <ul>
        <li>Interpret unverified account and post a re-verify link (login page)</li>
        <li>Forgot Password?</li>
      </ul>
    </div>

  </main>

  <footer>

  </footer>
</template>

<style lang="less">
@linkColor: #0000FF;

header {
  line-height: 1.5;
  max-height: 100vh;
}

h1,
h2,
h3,
h4,
h5,
h6 {
  text-align: center;
}

.login.working {
  background: red;
}

form .messages p {
  color: red;
  display: none;
}

form.has-error {
  .messages p {
    display: block;
  }
}

button.link-button {
  background: none;
  border: none;
  text-decoration: underline;

  color: @linkColor;
  margin: 0;
  padding: 0;
}

button.link-button:hover {
  cursor: pointer;
}

.todo {
  margin-top: 2rem;
  border: solid 1px pink;
  padding: 1rem;
  text-align: left !important;

  h1,
  h2,
  h3,
  h4,
  h5,
  h6 {
    text-align: initial;
  }

}</style>
