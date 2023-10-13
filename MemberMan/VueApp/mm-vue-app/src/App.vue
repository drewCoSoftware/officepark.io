<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
import HelloWorld from './components/HelloWorld.vue'
import type { IStatusData } from "./fetchy.js";
import { useLoginStore } from './stores/login';
import type { ILoginState } from "./stores/login";
import {ref} from 'vue';

const _Login = useLoginStore();

// For each page, or every so often we want to update the login status...
// NOTE: The back-end of the application is responsible for evauluating the
// permissions of the current user.
const loginState = _Login._CurrentState;
//  ?? 
//   ref({
//     IsLoggedIn:false,
//     DisplayName: "x",
//     Avatar: "x"
//   });

function updateLogin() {

//  alert('doing login...');
  const res = _Login.LoginUser("x", "y");
  if (!res[0]) {
    alert('there was a login error!');
  }
  else{
    // I dunno.......
  }
  // // NOTE: This approach will correctly update the state...
  // console.log('updating');
  // loginState.value = {
  //   IsLoggedIn : true,
  //   DisplayName : "wow",
  //   Avatar: "some-avatar.jpg"
  // }
}

</script>



<template>
  <header class="login-header">
    <div v-if="loginState?.IsLoggedIn">
      <img :src="loginState.Avatar" alt="avatar image" /> 
      <p>{{loginState.DisplayName}}</p>
      <button class="link" @click="_Login.Logout">Log Out</button>
    </div>
    <div v-else>
      <p>Not Logged In</p>
      <button @click="updateLogin">Push Me</button>
    </div>
    <!-- <p>{{loginState.DisplayName}}</p>
    <button @click="updateLogin">Push Me</button> -->
    <!-- <p>This is where the current login status will go + logout links, etc.</p> -->
  </header>

  <main>
    <div class="title">
      <!-- <HelloWorld msg="You did it!" /> -->
      <h1>Member Man Tester</h1>
      <nav>
        <RouterLink to="/">Home</RouterLink>
        <RouterLink to="/about">About</RouterLink>
      </nav>
    </div>

    <div class="login">
      <form>
        <div class="input">
          <label for="username">Username</label>
          <input type="text" name="username" />
        </div>
        <div class="input">
          <label for="username">Password</label>
          <input type="password" name="password" />
        </div>
        <button type="button">Login</button>
      </form>
    </div>
  </main>

  <footer>

  </footer>
  <!-- <RouterView /> -->
</template>

<style scoped>
header {
  line-height: 1.5;
  max-height: 100vh;
}

/* .logo {
  display: block;
  margin: 0 auto 2rem;
}

nav {
  width: 100%;
  font-size: 12px;
  text-align: center;
  margin-top: 2rem;
}

nav a.router-link-exact-active {
  color: var(--color-text);
}

nav a.router-link-exact-active:hover {
  background-color: transparent;
}

nav a {
  display: inline-block;
  padding: 0 1rem;
  border-left: 1px solid var(--color-border);
}

nav a:first-of-type {
  border: 0;
}

@media (min-width: 1024px) {
  header {
    display: flex;
    place-items: center;
    padding-right: calc(var(--section-gap) / 2);
  }

  .logo {
    margin: 0 2rem 0 0;
  }

  header .wrapper {
    display: flex;
    place-items: flex-start;
    flex-wrap: wrap;
  }

  nav {
    text-align: left;
    margin-left: -1rem;
    font-size: 1rem;

    padding: 1rem 0;
    margin-top: 1rem;
  } 
} */
</style>
